namespace Pathetic.Renderer.Shaders;

public readonly record struct ShaderParameter(string Name, ShaderResourceKind Kind, uint Slot, uint Space);

public sealed class ShaderModule
{
    public required string Name { get; init; }
    public required uint GroupSizeX { get; init; }
    public required uint GroupSizeY { get; init; }
    public required uint GroupSizeZ { get; init; }
    public required string SpirvResourceName { get; init; }
    public required IReadOnlyList<ShaderParameter> Parameters { get; init; }
}
