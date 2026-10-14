using Pathetic.Renderer.Resources;

namespace Pathetic.Renderer.Buffers;

public interface IPrimitiveBuffer : IDynamicBufferSource
{
    string Name { get; }
    uint Stride { get; }
}
