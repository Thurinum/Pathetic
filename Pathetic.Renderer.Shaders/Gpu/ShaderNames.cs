namespace Pathetic.Renderer.Shaders.Gpu;

public readonly record struct TextureName(string Value)
{
    public static implicit operator string(TextureName value) => value.Value;
}

public readonly record struct MutableTextureName(string Value)
{
    public static implicit operator string(MutableTextureName value) => value.Value;
    public static implicit operator TextureName(MutableTextureName value) => new(value.Value);
}

public readonly record struct SamplerName(string Value)
{
    public static implicit operator string(SamplerName value) => value.Value;
}

public readonly record struct UniformBufferName(string Value)
{
    public static implicit operator string(UniformBufferName value) => value.Value;
}

public readonly record struct BufferName(string Value)
{
    public static implicit operator string(BufferName value) => value.Value;
}

public readonly record struct MutableBufferName(string Value)
{
    public static implicit operator string(MutableBufferName value) => value.Value;
    public static implicit operator BufferName(MutableBufferName value) => new(value.Value);
}
