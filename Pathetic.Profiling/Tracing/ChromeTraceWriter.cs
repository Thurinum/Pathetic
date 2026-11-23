using System.Diagnostics;
using System.Text.Json;

namespace Pathetic.Profiling.Tracing;

/// <summary>Streams spans as Chrome Trace Event JSON, which chrome://tracing and ui.perfetto.dev can open and aggregate.</summary>
internal sealed class ChromeTraceWriter : IDisposable
{
    private const int ProcessId = 1;
    private const int GpuThreadId = 0;
    private const double TicksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000.0;

    private readonly FileStream _file;
    private readonly Utf8JsonWriter _json;
    private readonly DateTime _origin = DateTime.UtcNow;
    private readonly Lock _gate = new();

    public ChromeTraceWriter(string path)
    {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (directory is not null)
            Directory.CreateDirectory(directory);

        _file = File.Create(path);
        _json = new Utf8JsonWriter(_file);
        _json.WriteStartArray();
        WriteThreadName(GpuThreadId, "GPU");
    }

    public void Write(Activity span)
    {
        bool isGpu = span.OperationName.StartsWith(ProfilingName.GpuSpanPrefix, StringComparison.Ordinal);
        int threadId = isGpu ? GpuThreadId : Environment.CurrentManagedThreadId;

        lock (_gate)
        {
            _json.WriteStartObject();
            _json.WriteString("name", span.OperationName);
            _json.WriteString("cat", isGpu ? "gpu" : "cpu");
            _json.WriteString("ph", "X");
            _json.WriteNumber("ts", (span.StartTimeUtc - _origin).Ticks / TicksPerMicrosecond);
            _json.WriteNumber("dur", span.Duration.Ticks / TicksPerMicrosecond);
            _json.WriteNumber("pid", ProcessId);
            _json.WriteNumber("tid", threadId);
            WriteTags(span);
            _json.WriteEndObject();
            _json.Flush();
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _json.WriteEndArray();
            _json.Dispose();
            _file.Dispose();
        }
    }

    private void WriteTags(Activity span)
    {
        _json.WriteStartObject("args");
        foreach (KeyValuePair<string, object?> tag in span.TagObjects)
            _json.WriteString(tag.Key, $"{tag.Value}");

        _json.WriteEndObject();
    }

    private void WriteThreadName(int threadId, string name)
    {
        _json.WriteStartObject();
        _json.WriteString("name", "thread_name");
        _json.WriteString("ph", "M");
        _json.WriteNumber("pid", ProcessId);
        _json.WriteNumber("tid", threadId);
        _json.WriteStartObject("args");
        _json.WriteString("name", name);
        _json.WriteEndObject();
        _json.WriteEndObject();
    }
}
