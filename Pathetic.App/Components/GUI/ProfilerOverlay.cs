using System.Numerics;
using ImGuiNET;
using Pathetic.Profiling.Statistics;
using Pathetic.SceneGraph.Components;

namespace Pathetic.App.Components.GUI;

public sealed class ProfilerOverlay(ProfilerStats stats, ShaderCounterStats shaderCounters) : Component
{
    private const float GraphHeight = 60f;
    private const float Margin = 10f;
    private const uint SearchMaxLength = 128;

    private static readonly TimeSpan RefreshEvery = TimeSpan.FromMilliseconds(250);
    private static readonly Vector2 DefaultSize = new(680, 720);
    private static readonly Vector2 MinimumSize = new(460, 240);
    private static readonly Vector2 FullWidth = new(-1, 0);
    private static readonly (string Name, float Weight)[] SpanColumns =
        [("span", 3f), ("mean ms", 1f), ("max ms", 1f), ("calls/frame", 1f), ("ms/frame", 1f), ("share of frame", 2f)];
    private static readonly (string Name, float Weight)[] CounterColumns = [("counter", 2f), ("value", 1f)];
    private static readonly (string Name, float Weight)[] StateColumns = [("span", 2f), ("tag", 2f), ("value", 1f)];
    private static readonly (string Name, float Weight)[] EventColumns = [("event", 3f), ("last ms", 1f)];

    private ProfilerSnapshot _snapshot = stats.Snapshot();
    private IReadOnlyList<ShaderCounterRow> _counters = [];
    private long _refreshedAt;
    private string _search = string.Empty;

    protected override void GUI()
    {
        Refresh();

        ImGui.SetNextWindowPos(new Vector2(ImGui.GetIO().DisplaySize.X - DefaultSize.X - Margin, Margin), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(DefaultSize, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(MinimumSize, new Vector2(float.MaxValue));

        if (ImGui.Begin("Profiler"))
        {
            ImGui.Text($"{_snapshot.Fps:F1} fps   frame {_snapshot.FrameMeanMs:F2} ms");
            DrawFrameGraph();

            ImGui.InputTextWithHint("##search", "Search", ref _search, SearchMaxLength);

            if (ImGui.BeginTabBar("##profilerTabs"))
            {
                DrawTab("Timings", DrawTimings);
                DrawTab("Counters", DrawCounters);
                DrawTab("State", DrawState);
                ImGui.EndTabBar();
            }
        }

        ImGui.End();
    }

    private static void DrawTab(string title, Action draw)
    {
        if (!ImGui.BeginTabItem(title))
            return;

        draw();
        ImGui.EndTabItem();
    }

    private void Refresh()
    {
        if (TimeProvider.System.GetElapsedTime(_refreshedAt) < RefreshEvery)
            return;

        _snapshot = stats.Snapshot();
        _counters = shaderCounters.Rows();
        _refreshedAt = TimeProvider.System.GetTimestamp();
    }

    private bool Matches(string text) => _search.Length == 0 || text.Contains(_search, StringComparison.OrdinalIgnoreCase);

    private void DrawFrameGraph()
    {
        float[] history = _snapshot.FrameHistoryMs;
        if (history.Length == 0)
            return;

        float max = history.Max();
        ImGui.PlotLines("##frames", ref history[0], history.Length, 0, $"max {max:F1} ms", 0f, max, new Vector2(-1, GraphHeight));
    }

    private void DrawTimings()
    {
        DrawSpans("GPU passes", _snapshot.Spans.Where(span => span.IsGpu && Matches(span.Name)));
        DrawSpans("CPU spans", _snapshot.Spans.Where(span => !span.IsGpu && Matches(span.Name)));
        DrawEvents();
    }

    private void DrawCounters()
    {
        if (_counters.Count == 0)
        {
            ImGui.TextDisabled("No shader counters yet.");
            return;
        }

        if (!BeginTable("counters", CounterColumns))
            return;

        foreach (ShaderCounterRow row in _counters.Where(row => Matches(row.Name)))
        {
            ImGui.TableNextRow();
            Cell(row.Name);
            Cell(row.Value);
        }

        ImGui.EndTable();
    }

    private void DrawState()
    {
        if (!BeginTable("state", StateColumns))
            return;

        foreach (SpanStats span in _snapshot.Spans.Where(span => span.Tags.Count > 0))
        {
            foreach (SpanTag tag in span.Tags.Where(tag => Matches(span.Name) || Matches(tag.Key)))
            {
                ImGui.TableNextRow();
                Cell(span.Name);
                Cell(tag.Key);
                Cell(tag.Value);
            }
        }

        ImGui.EndTable();
    }

    private void DrawSpans(string title, IEnumerable<SpanStats> spans)
    {
        if (!ImGui.CollapsingHeader(title, ImGuiTreeNodeFlags.DefaultOpen) || !BeginTable(title, SpanColumns))
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
        SpanStats[] events = [.. _snapshot.Events.Where(span => Matches(span.Name))];
        if (events.Length == 0 || !ImGui.CollapsingHeader("Events", ImGuiTreeNodeFlags.DefaultOpen) || !BeginTable("events", EventColumns))
            return;

        foreach (SpanStats span in events)
        {
            ImGui.TableNextRow();
            Cell(span.Name);
            Cell($"{span.LastMs:F2}");
        }

        ImGui.EndTable();
    }

    private static bool BeginTable(string id, (string Name, float Weight)[] columns)
    {
        if (!ImGui.BeginTable(id, columns.Length, ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
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
