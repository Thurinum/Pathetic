using System.Runtime.InteropServices;
using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.RenderPasses;

[StructLayout(LayoutKind.Sequential, Size = 8)]
public record struct SelectionEntry
{
    public uint Primitive;
    public uint Index;
}

public sealed class EditorSelectionMaskPass : RenderPass
{
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    private struct Params
    {
        public uint Count;
    }

    public override string ShaderModule => "EditorSelectionMask";

    public override IReadOnlyList<IResourceSpec> Resources { get; } =
    [
        UniformBufferSpec.Of<Params>(ResourceName.Params),
    ];

    public override IReadOnlyList<string> Inputs { get; } =
    [
        ResourceName.Selections,
        ResourceName.Spheres,
        ResourceName.Planes,
    ];

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        uint count = ctx.Resources.GetElementCount(ResourceName.Selections);
        ctx.Cmd.UpdateBuffer(buffer, 0, new Params { Count = count });
    }
}
