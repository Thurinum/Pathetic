using Pathetic.Renderer.Buffers;

namespace Pathetic.SceneGraph;

public abstract class RendererBase<T>(SparseSet<T> primitives) : Component.Component where T : unmanaged, IEquatable<T>
{
    private Handle _handle = Handle.Invalid;

    protected override void Setup()
    {
        _handle = primitives.Add(BuildPrimitive());
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
            primitives.Remove(ref _handle);
        }
    }

    protected abstract T BuildPrimitive();
}