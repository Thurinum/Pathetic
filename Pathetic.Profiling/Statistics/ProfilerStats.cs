using System.Collections.Concurrent;

namespace Pathetic.Profiling.Statistics;

public sealed class ProfilerStats
{
    private const int WindowSize = 240;

    private sealed class Series
    {
        private readonly ConcurrentQueue<double> _recent = new();

        public long Count { get; private set; }
        public double Last { get; private set; }
        public string Tags { get; private set; } = string.Empty;
        public long LastSeen { get; private set; }

        public double[] Recent => [.. _recent];

        public void Add(double ms, string tags)
        {
            _recent.Enqueue(ms);
            while (_recent.Count > WindowSize)
                _recent.TryDequeue(out _);

            Count++;
            Last = ms;
            Tags = tags;
            LastSeen = TimeProvider.System.GetTimestamp();
        }
    }

    private readonly ConcurrentDictionary<string, Series> _series = new();
    private readonly Series _frames = new();

    internal long Add(string span, double ms, string tags)
    {
        Series series = _series.GetOrAdd(span, _ => new Series());
        series.Add(ms, tags);
        return series.Count;
    }

    internal void AddFrame(double ms) => _frames.Add(ms, tags: string.Empty);

    public ProfilerSnapshot Snapshot()
    {
        double[] frames = _frames.Recent;
        double frameMean = frames.DefaultIfEmpty().Average();
        List<SpanStats> spans = [.. _series.Select(entry => ToStats(entry.Key, entry.Value))];

        return new ProfilerSnapshot(
            Fps: frameMean > 0 ? TimeSpan.FromSeconds(1) / TimeSpan.FromMilliseconds(frameMean) : 0,
            FrameMeanMs: frameMean,
            FrameHistoryMs: [.. frames.Select(ms => (float)ms)],
            Spans: [.. spans.Where(span => !span.IsEvent).OrderByDescending(span => span.MeanMs)],
            Events: [.. spans.Where(span => span.IsEvent).OrderBy(span => span.Age)]);
    }

    private SpanStats ToStats(string name, Series series)
    {
        double[] recent = series.Recent;
        return new SpanStats(
            name,
            series.Last,
            recent.DefaultIfEmpty().Average(),
            recent.DefaultIfEmpty().Max(),
            CallsPerFrame: _frames.Count > 0 ? (double)series.Count / _frames.Count : 0,
            series.Tags,
            Age: TimeProvider.System.GetElapsedTime(series.LastSeen));
    }
}
