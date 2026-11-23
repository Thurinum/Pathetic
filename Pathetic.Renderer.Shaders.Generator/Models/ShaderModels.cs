namespace Pathetic.Renderer.Shaders.Generator.Models;

internal enum BindingKind
{
    Texture,
    MutableTexture,
    Sampler,
    UniformBuffer,
    Buffer,
    MutableBuffer,
}

internal sealed record ModuleModel(
    string ModuleName,
    PipelineModel? Pipeline,
    IReadOnlyList<ResourceBinding> Bindings,
    IReadOnlyList<TypeModel> Types);

internal sealed record PipelineModel(
    ThreadGroupSize ThreadGroupSize,
    IReadOnlyList<PipelineParameter> Parameters);

internal readonly record struct ThreadGroupSize(uint X, uint Y, uint Z);

internal sealed record ResourceBinding(string Name, BindingKind Kind);

internal sealed record PipelineParameter(string Name, BindingKind Kind, uint Slot, uint Space);

internal sealed record TypeModel(string Name, int Size, IReadOnlyList<FieldModel> Fields);

internal sealed record FieldModel(string Name, int Offset, string CsType);
