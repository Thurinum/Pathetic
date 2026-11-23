namespace Pathetic.Renderer.Shaders.GPUTypes;

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
