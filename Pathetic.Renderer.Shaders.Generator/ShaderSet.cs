namespace Pathetic.Renderer.Shaders.Generator;

internal sealed class ShaderFile
{
    public required string Path { get; init; }
    public required string ModuleName { get; init; }
}

internal sealed class ShaderSet
{
    public required IReadOnlyList<ShaderFile> Files { get; init; }
    public required IReadOnlyList<string> Directories { get; init; }

    public static ShaderSet Discover(IReadOnlyList<string> directories)
    {
        List<ShaderFile> files = [];
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

                files.Add(new ShaderFile
                {
                    Path = path,
                    ModuleName = moduleName,
                });
            }
        }

        if (files.Count == 0)
            throw new ShaderCompilerException("No shader files found.");

        files.Sort(static (a, b) => string.CompareOrdinal(a.ModuleName, b.ModuleName));

        return new ShaderSet { Files = files, Directories = directories };
    }
}
