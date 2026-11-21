using Pathetic.Renderer.Graph;

namespace Pathetic.App.RenderPasses;

public sealed class EditorSelectionOutlinePass : RenderPass
{
    public override string ShaderModule => "EditorSelectionOutline";

    public override IReadOnlyList<string> Inputs { get; } =
    [
        ResourceName.SelectionMask,
    ];
}
