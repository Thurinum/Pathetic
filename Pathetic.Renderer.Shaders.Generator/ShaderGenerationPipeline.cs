using Pathetic.Renderer.Shaders.Generator.Compilation;
using Pathetic.Renderer.Shaders.Generator.Discovery;
using Pathetic.Renderer.Shaders.Generator.Generation;
using Pathetic.Renderer.Shaders.Generator.Models;

namespace Pathetic.Renderer.Shaders.Generator;

internal static class ShaderGenerationPipeline
{
    public static void Run(ShaderGenerationOptions options)
    {
        ShaderSourceSet sources = ShaderSourceDiscovery.Discover(options.ShaderDirectories);
        ShaderCompilationResult result = SlangShaderCompiler.Compile(sources, options.ProfileCounters);
        GeneratedSources generated = CSharpSourceEmitter.Emit(result.Modules);

        ShaderArtifactWriter.Write(options.OutputDirectory, result, generated);

        Console.WriteLine($"Pathetic.Renderer.Shaders.Generator: {sources.Sources.Count} module(s) written to '{options.OutputDirectory}'.");
    }
}
