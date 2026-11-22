namespace Pathetic.Slang.Tool;

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

    public static ShaderSet Discover(IEnumerable<string> roots)
    {
        List<ShaderFile> files = [];
        List<string> resolvedRoots = [];
        HashSet<string> stems = new(StringComparer.OrdinalIgnoreCase);

        foreach (string root in roots)
        {
            if (!Directory.Exists(root))
                throw new ToolException($"Shader directory '{root}' does not exist.");

            resolvedRoots.Add(root);

            foreach (string path in Directory.EnumerateFiles(root, "*.slang", SearchOption.AllDirectories))
            {
                if (IsIncludeFile(root, path))
                    continue;

                string stem = System.IO.Path.GetFileNameWithoutExtension(path);

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

        return new ShaderSet { Files = files, Roots = resolvedRoots };
    }

    private static bool IsIncludeFile(string root, string path)
    {
        string relative = System.IO.Path.GetRelativePath(root, path);
        string[] segments = relative.Split(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
        return segments.Length > 1 && string.Equals(segments[0], "include", StringComparison.OrdinalIgnoreCase);
    }
}
