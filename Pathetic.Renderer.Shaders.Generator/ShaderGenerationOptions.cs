namespace Pathetic.Renderer.Shaders.Generator;

internal sealed record ShaderGenerationOptions(
    IReadOnlyList<string> ShaderDirectories,
    string OutputDirectory,
    bool ProfileCounters);
