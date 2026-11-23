using System.Text;
using Pathetic.Renderer.Shaders.Generator.Models;

namespace Pathetic.Renderer.Shaders.Generator.Generation;

internal static class ShaderArtifactWriter
{
    private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

    public static void Write(string outDir, ShaderCompilationResult result, GeneratedSources generated)
    {
        string codeDir = Path.Combine(outDir, "gen");
        Directory.CreateDirectory(codeDir);

        WriteGeneratedFiles(codeDir, generated);
        WriteSpirv(outDir, result);
    }

    private static void WriteGeneratedFiles(string codeDir, GeneratedSources generated)
    {
        foreach ((string moduleName, string source) in generated.SourceByModule)
            File.WriteAllText(Path.Combine(codeDir, moduleName + ".g.cs"), source, Utf8WithBom);

        File.WriteAllText(Path.Combine(codeDir, "ShaderModules.g.cs"), generated.Registry, Utf8WithBom);
    }

    private static void WriteSpirv(string outDir, ShaderCompilationResult result)
    {
        foreach ((string moduleName, byte[] code) in result.SpirvByModule)
            File.WriteAllBytes(Path.Combine(outDir, moduleName + ".spv"), code);
    }
}
