namespace Pathetic.Renderer.Shaders.Generator;

internal enum BindingKind
{
    Texture,
    MutableTexture,
    Sampler,
    UniformBuffer,
    Buffer,
    MutableBuffer,
}

internal sealed class ModuleModel
{
    public required string Name { get; init; }
    public PipelineModel? Pipeline { get; set; }
    public List<BindingModel> Bindings { get; init; } = [];
    public List<TypeModel> Types { get; init; } = [];
}

internal sealed class PipelineModel
{
    public required uint[] ThreadGroupSize { get; init; }
    public List<ParameterModel> Parameters { get; init; } = [];
}

internal sealed class BindingModel
{
    public required string Name { get; init; }
    public required BindingKind Kind { get; init; }
}

internal sealed class ParameterModel
{
    public required string Name { get; init; }
    public required BindingKind Kind { get; init; }
    public required uint Space { get; init; }
    public required uint Binding { get; init; }
}

internal sealed class TypeModel
{
    public required string Name { get; init; }
    public required int Size { get; init; }
    public List<FieldModel> Fields { get; init; } = [];
}

internal sealed class FieldModel
{
    public required string Name { get; init; }
    public int Offset { get; init; }
    public required string CsType { get; init; }
}
