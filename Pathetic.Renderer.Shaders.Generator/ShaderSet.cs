namespace Pathetic.Renderer.Shaders.Generator;

internal sealed class ShaderFile
{
    public required string Path { get; init; }
    public required string Name { get; init; }
}

internal sealed class ShaderSet
{
    public required IReadOnlyList<ShaderFile> Files { get; init; }
    public required IReadOnlyList<string> Roots { get; init; }

    public static ShaderSet Discover(IReadOnlyList<string> roots)
    {
        List<ShaderFile> files = [];
        HashSet<string> stems = new(StringComparer.OrdinalIgnoreCase);

        foreach (string root in roots)
        {
            if (!Directory.Exists(root))
                throw new SlangInteropException($"Shader directory '{root}' does not exist.");

            foreach (string path in Directory.EnumerateFiles(root, "*.slang", SearchOption.AllDirectories))
            {
                string stem = Path.GetFileNameWithoutExtension(path);

                if (!stems.Add(stem))
                    throw new SlangInteropException($"Two shader files share the name '{stem}', outputs would collide.");

                files.Add(new ShaderFile
                {
                    Path = path,
                    Name = stem,
                });
            }
        }

        if (files.Count == 0)
            throw new SlangInteropException("No shader files found.");

        files.Sort(static (a, b) => string.CompareOrdinal(a.Name, b.Name));

        return new ShaderSet { Files = files, Roots = roots };
    }
}
