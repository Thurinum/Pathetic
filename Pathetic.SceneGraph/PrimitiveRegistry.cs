using Pathetic.Renderer.Buffers;
using Pathetic.SceneGraph.Components;

namespace Pathetic.SceneGraph;

public sealed class PrimitiveRegistry
{
    private readonly Dictionary<Handle, Component> _owners = [];
    public void Register(Handle handle, Component owner) => _owners[handle] = owner;

    public void Unregister(Handle handle) => _owners.Remove(handle);

    public Component? OwnerOf(Handle handle) => _owners.GetValueOrDefault(handle);
}
