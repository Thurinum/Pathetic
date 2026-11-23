namespace Pathetic.Renderer.Resources;

[Flags]
public enum SceneChange : uint
{
    None = 0,

    /// <summary>Element count or order changed (add, remove, reallocate).</summary>
    Structure = 1 << 0,

    /// <summary>Element values changed.</summary>
    Content = 1 << 1,

    /// <summary>A GPU resource was recreated, so its previous contents are gone.</summary>
    Allocation = 1 << 2,

    /// <summary>Shader constants derived from settings changed.</summary>
    Parameters = 1 << 3,

    All = Structure | Content | Allocation | Parameters,
}
