namespace Pathetic.Renderer.Shaders.Generator.Models;

internal sealed record ShaderSource(string Path, string ModuleName);

internal sealed record ShaderSourceSet(IReadOnlyList<ShaderSource> Sources, IReadOnlyList<string> Directories);
