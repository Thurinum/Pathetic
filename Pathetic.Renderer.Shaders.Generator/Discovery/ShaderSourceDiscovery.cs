using Pathetic.Renderer.Shaders.Generator.Diagnostics;
using Pathetic.Renderer.Shaders.Generator.Models;

namespace Pathetic.Renderer.Shaders.Generator.Discovery;

internal static class ShaderSourceDiscovery
{
    public static ShaderSourceSet Discover(IReadOnlyList<string> directories)
    {
        List<ShaderSource> sources = [];
        HashSet<string> moduleNames = new(StringComparer.OrdinalIgnoreCase);

        foreach (string directory in directories)
        {
            if (!Directory.Exists(directory))
                throw new ShaderCompilerException($"Shader directory '{directory}' does not exist.");

            foreach (string path in Directory.EnumerateFiles(directory, "*.slang", SearchOption.AllDirectories))
            {
                string moduleName = Path.GetFileNameWithoutExtension(path);

                if (!moduleNames.Add(moduleName))
                    throw new ShaderCompilerException($"Two shader files share the name '{moduleName}', outputs would collide.");

                sources.Add(new ShaderSource(path, moduleName));
            }
        }

        if (sources.Count == 0)
            throw new ShaderCompilerException("No shader files found.");

        sources.Sort(static (a, b) => string.CompareOrdinal(a.ModuleName, b.ModuleName));

        return new ShaderSourceSet(sources, directories);
    }
}
