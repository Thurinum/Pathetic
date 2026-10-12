using NeoVeldrid;

namespace Pathetic.Renderer.Resources;

public sealed class RenderGraphResource
{
    public required IResourceSpec Spec { get; set; }

    public BindableResource? Resource
    {
        get;
        set
        {
            (field as IDisposable)?.Dispose();
            field = value;
        }
    }

    public uint ElementCapacity { get; set; }
}