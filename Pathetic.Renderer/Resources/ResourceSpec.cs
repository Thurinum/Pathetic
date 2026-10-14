using System.Runtime.CompilerServices;
using NeoVeldrid;

namespace Pathetic.Renderer.Resources;

// outputs
public interface IResourceSpec { string Name { get; } }
public sealed record TextureSpec(string Name, PixelFormat Format, TextureUsage Usage, TextureSizePolicy SizePolicy) : IResourceSpec;
public sealed record SamplerSpec(string Name, SamplerDescription Description) : IResourceSpec;
public sealed record UniformBufferSpec : IResourceSpec
{
    public string Name { get; }
    public uint SizeInBytes { get; }
    
    private UniformBufferSpec(string name, uint size)   // only ctor, and private
    {
        Name = name;
        SizeInBytes = size;
    }
    
    public static UniformBufferSpec Of<T>(string name) where T : unmanaged
    {
        uint size = (uint)Unsafe.SizeOf<T>();
        uint aligned16 = (size + 15u) & ~15u;
        return new UniformBufferSpec(name, aligned16);
    }
}
public sealed record StructuredBufferSpec(string Name, uint Stride, uint ElementCount) : IResourceSpec;
public interface IDynamicBufferSpec : IResourceSpec
{
    uint Stride { get; }
    uint ElementCount { get; }
    void Upload(CommandList cmd, DeviceBuffer buf, bool resized);
}

