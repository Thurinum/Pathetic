using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Pathetic.Profiling.Statistics;

public sealed class ProfilerStats(IOptions<ProfilerOptions> options)
{
    private const int MaxEntriesPerSeries = 1 << 16;
    private const double EmaWeight = 0.2;

    private sealed class Series
    {
        private readonly Queue<(long At, double Ms)> _entries = new();
        private readonly Lock _gate = new();

        public long Count { get; private set; }
        public double Last { get; private set; }
        public IReadOnlyList<SpanTag> Tags { get; private set; } = [];
        public long LastSeen { get; private set; }
        public long LastFrame { get; private set; }

        /// <summary>Exponential moving average of the time between two occurrences, in milliseconds. Zero until known.</summary>
        public double IntervalMs { get; private set; }

        public void Add(double ms, IReadOnlyList<SpanTag> tags, long windowStart, long frame)
        {
            long now = TimeProvider.System.GetTimestamp();

            lock (_gate)
            {
                if (Count > 0)
                {
                    double interval = TimeProvider.System.GetElapsedTime(LastSeen, now).TotalMilliseconds;
                    IntervalMs = IntervalMs == 0 ? interval : IntervalMs + EmaWeight * (interval - IntervalMs);
                }

                _entries.Enqueue((now, ms));
                Prune(windowStart);

                Count++;
                Last = ms;
                Tags = tags;
                LastSeen = now;
                LastFrame = frame;
            }
        }

        public double[] Window(long windowStart)
        {
            lock (_gate)
            {
                Prune(windowStart);
                return [.. _entries.Select(entry => entry.Ms)];
            }
        }

        private void Prune(long windowStart)
        {
            while (_entries.Count > 0 && (_entries.Peek().At < windowStart || _entries.Count > MaxEntriesPerSeries))
                _entries.Dequeue();
        }
    }

    private readonly int _windowFrames = options.Value.WindowFrames;
    private readonly ConcurrentDictionary<string, Series> _series = new();
    private readonly Lock _frameGate = new();
    private readonly Queue<long> _boundaries = new();
    private readonly Queue<double> _frameMs = new();

    private long _windowStart;
    private long _frameCount;
    private long _lastBoundary;

    internal long Add(string span, double ms, IReadOnlyList<SpanTag> tags)
    {
        Series series = _series.GetOrAdd(span, _ => new Series());
        series.Add(ms, tags, Volatile.Read(ref _windowStart), Volatile.Read(ref _frameCount));
        return series.Count;
    }

    /// <summary>Marks the end of a frame. Statistics only cover the last <see cref="ProfilerOptions.WindowFrames"/> of them.</summary>
    internal void AddFrame()
    {
        long now = TimeProvider.System.GetTimestamp();

        lock (_frameGate)
        {
            if (_boundaries.Count > 0)
            {
                _frameMs.Enqueue(TimeProvider.System.GetElapsedTime(_lastBoundary, now).TotalMilliseconds);
                if (_frameMs.Count > _windowFrames)
                    _frameMs.Dequeue();
            }

            _lastBoundary = now;
            Interlocked.Increment(ref _frameCount);
            _boundaries.Enqueue(now);
            if (_boundaries.Count > _windowFrames + 1)
                _boundaries.Dequeue();

            Volatile.Write(ref _windowStart, _boundaries.Peek());
        }
    }

    public ProfilerSnapshot Snapshot()
    {
        double[] frames;
        lock (_frameGate)
            frames = [.. _frameMs];

        double frameMean = frames.DefaultIfEmpty().Average();
        long windowStart = Volatile.Read(ref _windowStart);
        long currentFrame = Volatile.Read(ref _frameCount);
        TimeSpan windowDuration = TimeSpan.FromMilliseconds(frameMean * frames.Length);
        List<(SpanStats Stats, long LastSeen)> spans =
            [.. _series.Select(entry => ToStats(entry.Key, entry.Value, windowStart, frames.Length, windowDuration, currentFrame))];

        return new ProfilerSnapshot(
            Fps: frameMean > 0 ? TimeSpan.FromSeconds(1) / TimeSpan.FromMilliseconds(frameMean) : 0,
            FrameMeanMs: frameMean,
            FrameHistoryMs: [.. frames.Select(ms => (float)ms)],
            Spans: [.. spans.Where(span => !span.Stats.IsEvent).Select(span => span.Stats).OrderByDescending(span => span.MeanMs)],
            Events: [.. spans.Where(span => span.Stats.IsEvent).OrderByDescending(span => span.LastSeen).Select(span => span.Stats)]);
    }

    private static (SpanStats, long) ToStats(string name, Series series, long windowStart, int frameCount, TimeSpan windowDuration, long currentFrame)
    {
        double[] recent = series.Window(windowStart);
        TimeSpan interval = TimeSpan.FromMilliseconds(series.IntervalMs);

        // Something is an event, rather than recurring work, when it does not keep up with the profiled window.
        bool isEvent = series.Count < 2 || recent.Length == 0 || interval > windowDuration
                       || currentFrame - series.LastFrame > frameCount;

        return (new SpanStats(
            name,
            series.Last,
            recent.Length > 0 ? recent.Average() : series.Last,
            recent.Length > 0 ? recent.Max() : series.Last,
            CallsPerFrame: frameCount > 0 ? (double)recent.Length / frameCount : 0,
            series.Tags,
            isEvent), series.LastSeen);
    }
}
