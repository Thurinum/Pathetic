using Pathetic.App.Components;
using Pathetic.Renderer.Buffers;

namespace Pathetic.App;

public sealed class MaterialLibrary(StructuredBuffer<Material> materials)
{
    private readonly Dictionary<Material, uint> _ids = [];

    // TODO: materials are never evicted; animating a Material leaks one buffer entry per value.
    public uint GetOrAdd(Material material)
    {
        if (_ids.TryGetValue(material, out uint id))
            return id;

        id = (uint)materials.Add(material);
        _ids.Add(material, id);
        return id;
    }
}
