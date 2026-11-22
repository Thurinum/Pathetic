#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace Pathetic.Generated.GPUTypes;
#pragma warning restore IDE0130

public readonly record struct TextureRef(string Value)
{
    public static implicit operator string(TextureRef value) => value.Value;
}

public readonly record struct MutableTextureRef(string Value)
{
    public static implicit operator string(MutableTextureRef value) => value.Value;
    public static implicit operator TextureRef(MutableTextureRef value) => new(value.Value);
}

public readonly record struct SamplerRef(string Value)
{
    public static implicit operator string(SamplerRef value) => value.Value;
}

public readonly record struct UniformBufferRef(string Value)
{
    public static implicit operator string(UniformBufferRef value) => value.Value;
}

public readonly record struct BufferRef(string Value)
{
    public static implicit operator string(BufferRef value) => value.Value;
}

public readonly record struct MutableBufferRef(string Value)
{
    public static implicit operator string(MutableBufferRef value) => value.Value;
    public static implicit operator BufferRef(MutableBufferRef value) => new(value.Value);
}
