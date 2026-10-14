using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.Renderer.Buffers;

public sealed class PrimitiveResourceProvider(IEnumerable<IPrimitiveBuffer> buffers) : IResourceProvider
{
    public IEnumerable<IResourceSpec> Resources => buffers.Select(b => new DynamicBufferSpec(b.Name, b.Stride, b));
    public void Update(RenderContext ctx) { }
}
