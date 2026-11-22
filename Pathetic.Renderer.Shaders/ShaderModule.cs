namespace Pathetic.Renderer.Shaders;

public readonly record struct ShaderParameter(string Name, ShaderResourceKind Kind, uint Space, uint Binding);

public sealed class ShaderModule
{
    public required string Name { get; init; }
    public required uint GroupSizeX { get; init; }
    public required uint GroupSizeY { get; init; }
    public required uint GroupSizeZ { get; init; }
    public required string CodeResource { get; init; }
    public required IReadOnlyList<ShaderParameter> Parameters { get; init; }

    public byte[] LoadCode()
    {
        using Stream stream = typeof(ShaderModule).Assembly.GetManifestResourceStream(CodeResource)
            ?? throw new InvalidOperationException($"Shader module '{Name}' is missing its embedded SPIR-V resource '{CodeResource}'.");

        using MemoryStream buffer = new();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
