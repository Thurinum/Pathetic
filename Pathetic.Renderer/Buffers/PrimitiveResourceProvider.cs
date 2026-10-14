using Pathetic.Renderer.Resources;

namespace Pathetic.Renderer.Buffers;

public sealed class PrimitiveResourceProvider(IEnumerable<IDynamicBufferSpec> buffers) : IResourceProvider
{
    public IEnumerable<IResourceSpec> Resources => buffers;
}
