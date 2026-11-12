using Pathetic.Renderer.Buffers;
using Pathetic.SceneGraph.Components;

namespace Pathetic.SceneGraph;

public abstract class RendererBase<T>(SparseSet<T> primitives, PrimitiveRegistry registry)
    : Component where T : unmanaged, IEquatable<T>
{
    private Handle _handle = Handle.Invalid;

    protected override void Setup()
    {
        _handle = primitives.Add(BuildPrimitive());
        registry.Register(_handle, this);
    }

    protected override void LateUpdate(float deltaTime)
    {
        if (_handle.IsValid)
        {
            primitives.Update(_handle, BuildPrimitive());
        }
    }

    protected override void Cleanup()
    {
        if (_handle.IsValid)
        {
            registry.Unregister(_handle);
            primitives.Remove(ref _handle);
        }
    }

    protected abstract T BuildPrimitive();
}