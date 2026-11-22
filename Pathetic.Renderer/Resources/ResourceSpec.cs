using System.Runtime.CompilerServices;
using NeoVeldrid;
using Pathetic.Renderer.Shaders.GPUTypes;
using Pathetic.Renderer.Graph;

namespace Pathetic.Renderer.Resources;

// outputs
public interface IResourceSpec { string Name { get; } }
public sealed record TextureSpec : IResourceSpec
{
    public TextureSpec(TextureRef name, PixelFormat format, TextureUsage usage, TextureSizePolicy sizePolicy)
    {
        Name = name.Value;
        Format = format;
        Usage = usage;
        SizePolicy = sizePolicy;
    }

    public string Name { get; }
    public PixelFormat Format { get; }
    public TextureUsage Usage { get; }
    public TextureSizePolicy SizePolicy { get; }
}

public sealed record SamplerSpec : IResourceSpec
{
    public SamplerSpec(SamplerRef name, SamplerDescription description)
    {
        Name = name.Value;
        Description = description;
    }

    public string Name { get; }
    public SamplerDescription Description { get; }
}

public sealed record UniformBufferSpec : IResourceSpec
{
    public string Name { get; }
    public uint SizeInBytes { get; }
    
    private UniformBufferSpec(string name, uint size)   // only ctor, and private
    {
        Name = name;
        SizeInBytes = size;
    }
    
    public static UniformBufferSpec Of<T>(UniformBufferRef name) where T : unmanaged
    {
        uint size = (uint)Unsafe.SizeOf<T>();
        uint aligned16 = (size + 15u) & ~15u;
        return new UniformBufferSpec(name.Value, aligned16);
    }
}
public interface IResourceUploader
{
    IReadOnlyList<IResourceSpec> Resources { get; }

    SceneChange DirtyChanges { get; }

    void SetDirty(SceneChange changes);
    void ClearDirty();

    void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized);
}

public interface IStructuredBufferSpec : IResourceSpec, IResourceUploader
{
    uint Stride { get; }
    uint ElementCount { get; }
    uint InitialCapacity { get; }
    bool IsResizable { get; }
    bool IsDynamic { get; }
}

