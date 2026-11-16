using System.Numerics;
using ImGuiNET;
using Pathetic.Profiling.Statistics;

namespace Pathetic.Profiling.Overlay;

public sealed class ProfilerWindow(ProfilerStats stats, ShaderCounterStats shaderCounters)
{
    private const float GraphHeight = 60f;
    private const float Margin = 10f;

    private static readonly TimeSpan RefreshEvery = TimeSpan.FromMilliseconds(250);
    private static readonly Vector2 DefaultSize = new(680, 720);
    private static readonly Vector2 MinimumSize = new(460, 240);
    private static readonly Vector2 FullWidth = new(-1, 0);
    private static readonly (string Name, float Weight)[] SpanColumns =
        [("span", 3f), ("mean ms", 1f), ("max ms", 1f), ("calls/frame", 1f), ("ms/frame", 1f), ("share of frame", 2f)];
    private static readonly (string Name, float Weight)[] CounterColumns = [("counter", 2f), ("value", 1f)];

    private ProfilerSnapshot _snapshot = stats.Snapshot();
    private IReadOnlyList<(string Name, string Value)> _counters = [];
    private long _refreshedAt;

    public void Draw()
    {
        Refresh();

        ImGui.SetNextWindowPos(new Vector2(ImGui.GetIO().DisplaySize.X - DefaultSize.X - Margin, Margin), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(DefaultSize, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(MinimumSize, new Vector2(float.MaxValue));

        if (ImGui.Begin("Profiler"))
        {
            ImGui.Text($"{_snapshot.Fps:F1} fps   frame {_snapshot.FrameMeanMs:F2} ms");
            DrawFrameGraph();
            DrawCounters();
            DrawState();
            DrawSpans("GPU passes", _snapshot.Spans.Where(span => span.IsGpu));
            DrawSpans("CPU spans", _snapshot.Spans.Where(span => !span.IsGpu));
            DrawEvents();
        }

        ImGui.End();
    }

    private void Refresh()
    {
        if (TimeProvider.System.GetElapsedTime(_refreshedAt) < RefreshEvery)
            return;

        _snapshot = stats.Snapshot();
        _counters = shaderCounters.Rows();
        _refreshedAt = TimeProvider.System.GetTimestamp();
    }

    private void DrawFrameGraph()
    {
        float[] history = _snapshot.FrameHistoryMs;
        if (history.Length == 0)
            return;

        float max = history.Max();
        ImGui.PlotLines("##frames", ref history[0], history.Length, 0, $"max {max:F1} ms", 0f, max, new Vector2(-1, GraphHeight));
    }

    private void DrawCounters()
    {
        if (_counters.Count == 0 || !BeginSection("Path tracer (shader counters)", CounterColumns))
            return;

        foreach ((string name, string value) in _counters)
        {
            ImGui.TableNextRow();
            Cell(name);
            Cell(value);
        }

        ImGui.EndTable();
    }

    private void DrawState()
    {
        SpanStats[] tagged = [.. _snapshot.Spans.Where(span => span.Tags.Length > 0)];
        if (tagged.Length == 0 || !ImGui.CollapsingHeader("State", ImGuiTreeNodeFlags.DefaultOpen))
            return;

        foreach (SpanStats span in tagged)
            ImGui.TextWrapped($"{span.Name}  {span.Tags}");
    }

    private void DrawSpans(string title, IEnumerable<SpanStats> spans)
    {
        if (!BeginSection(title, SpanColumns))
            return;

        foreach (SpanStats span in spans)
        {
            ImGui.TableNextRow();
            Cell(span.Name);
            Cell($"{span.MeanMs:F3}");
            Cell($"{span.MaxMs:F3}");
            Cell($"{span.CallsPerFrame:F1}");
            Cell($"{span.TotalMsPerFrame:F3}");
            ImGui.TableNextColumn();
            DrawShare(span.TotalMsPerFrame);
        }

        ImGui.EndTable();
    }

    private void DrawShare(double msPerFrame)
    {
        float share = _snapshot.FrameMeanMs > 0 ? (float)Math.Min(msPerFrame / _snapshot.FrameMeanMs, 1) : 0f;
        ImGui.ProgressBar(share, FullWidth, $"{share:P0}");
    }

    private void DrawEvents()
    {
        if (_snapshot.Events.Count == 0 || !ImGui.CollapsingHeader("Events", ImGuiTreeNodeFlags.DefaultOpen))
            return;

        foreach (SpanStats span in _snapshot.Events)
            ImGui.TextWrapped($"{span.Name}  {span.LastMs:F2} ms  {span.Tags}");
    }

    private static bool BeginSection(string title, (string Name, float Weight)[] columns)
    {
        if (!ImGui.CollapsingHeader(title, ImGuiTreeNodeFlags.DefaultOpen))
            return false;

        if (!ImGui.BeginTable(title, columns.Length, ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
            return false;

        foreach ((string name, float weight) in columns)
            ImGui.TableSetupColumn(name, ImGuiTableColumnFlags.WidthStretch, weight);

        ImGui.TableHeadersRow();
        return true;
    }

    private static void Cell(string text)
    {
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(text);
    }
}
