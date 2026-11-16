using Pathetic.Profiling;
using Pathetic.Profiling.Overlay;
using Pathetic.SceneGraph.Components;

namespace Pathetic.App.Components.GUI;

public sealed class ProfilerOverlay(ProfilerWindow window) : Component
{
    protected override void GUI() => window.Draw();
}
