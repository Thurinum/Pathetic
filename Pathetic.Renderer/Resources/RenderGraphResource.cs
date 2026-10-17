using NeoVeldrid;

namespace Pathetic.Renderer.Resources;

public sealed class RenderGraphResource
{
    public required IResourceSpec Spec { get; init; }
    public IResourceUploader? Writer { get; init; }

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