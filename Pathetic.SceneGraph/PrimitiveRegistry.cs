using Pathetic.Renderer.Buffers;
using Pathetic.SceneGraph.Components;

namespace Pathetic.SceneGraph;

public sealed class PrimitiveRegistry
{
    private readonly Dictionary<(Type PrimitiveType, Handle Handle), Component> _owners = [];

    public void Register(Type primitiveType, Handle handle, Component owner) => _owners[(primitiveType, handle)] = owner;

    public void Unregister(Type primitiveType, Handle handle) => _owners.Remove((primitiveType, handle));

    public Component? OwnerOf(Type primitiveType, Handle handle) => _owners.GetValueOrDefault((primitiveType, handle));
}
