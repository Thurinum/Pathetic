using Pathetic.Renderer.Resources;

namespace Pathetic.Renderer.Primitives;

public interface IPrimitiveBuffer : IDynamicBufferSource
{
    string Name { get; }
    Type PrimitiveType { get; }
    int Capacity { get; }
    int Count { get; }
    uint Stride { get; }
}
