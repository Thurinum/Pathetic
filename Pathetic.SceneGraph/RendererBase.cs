using Pathetic.Renderer.Primitives;

namespace Pathetic.SceneGraph;

public abstract class RendererBase<T>(PrimitiveBuffer<T> primitives) : Component where T : unmanaged, IEquatable<T>
{
    private PrimitiveHandle _handle = PrimitiveHandle.Invalid;

    public override void Awake()
    {
        _handle = primitives.Add(BuildPrimitive());
    }

    public override void LateUpdate(float deltaTime)
    {
        if (_handle.IsValid)
        {
            primitives.Update(_handle, BuildPrimitive());
        }
    }

    public override void OnDestroy()
    {
        if (_handle.IsValid)
        {
            primitives.Remove(ref _handle);
        }
    }

    protected abstract T BuildPrimitive();
}