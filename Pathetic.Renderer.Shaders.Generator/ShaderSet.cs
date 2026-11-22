namespace Pathetic.Renderer.Shaders.Generator;

internal sealed class ShaderFile
{
    public required string Path { get; init; }
    public required string ModuleName { get; init; }
    public required string OutputStem { get; init; }
}

internal sealed class ShaderSet
{
    public required IReadOnlyList<ShaderFile> Files { get; init; }
    public required IReadOnlyList<string> Roots { get; init; }

    public static ShaderSet Discover(IEnumerable<string> ownedRoots, IEnumerable<string> searchRoots)
    {
        List<string> owned = [.. ownedRoots];
        List<string> roots = [];
        List<ShaderFile> files = [];
        HashSet<string> stems = new(StringComparer.OrdinalIgnoreCase);

        foreach (string root in owned.Concat(searchRoots))
        {
            if (!Directory.Exists(root))
                throw new ToolException($"Shader directory '{root}' does not exist.");

            if (!roots.Contains(root))
                roots.Add(root);
        }

        foreach (string root in owned)
        {
            foreach (string path in Directory.EnumerateFiles(root, "*.slang", SearchOption.AllDirectories))
            {
                string stem = Path.GetFileNameWithoutExtension(path);

                if (!stems.Add(stem))
                    throw new ToolException($"Two shader files share the name '{stem}', outputs would collide.");

                files.Add(new ShaderFile
                {
                    Path = path,
                    ModuleName = stem,
                    OutputStem = stem,
                });
            }
        }

        if (files.Count == 0)
            throw new ToolException("No shader files found.");

        files.Sort(static (a, b) => string.CompareOrdinal(a.OutputStem, b.OutputStem));

        return new ShaderSet { Files = files, Roots = roots };
    }
}
