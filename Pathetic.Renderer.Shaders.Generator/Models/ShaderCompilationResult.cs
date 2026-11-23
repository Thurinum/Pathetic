namespace Pathetic.Renderer.Shaders.Generator.Models;

internal sealed record ShaderCompilationResult(
    IReadOnlyList<ModuleModel> Modules,
    IReadOnlyDictionary<string, byte[]> SpirvByModule);

internal sealed record GeneratedSources(
    IReadOnlyDictionary<string, string> SourceByModule,
    string Registry);
