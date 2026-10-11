using NeoVeldrid;

namespace Pathetic.Renderer.Resources;

public sealed class RenderGraphResource
{
    public required IResourceSpec Spec;

    public BindableResource? Resource
    {
        get;
        set
        {
            (field as IDisposable)?.Dispose();
            field = value;
        }
    }

    public uint ElementCapacity;
}