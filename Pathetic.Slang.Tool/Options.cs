namespace Pathetic.Slang.Tool;

internal sealed class ToolException(string message) : Exception(message);

internal sealed class Options
{
    public List<string> ShaderDirs { get; } = [];
    public string OutDir { get; private set; } = "";
    public string Namespace { get; private set; } = "Pathetic.Generated.Slang";

    public static Options Parse(string[] args)
    {
        Options options = new();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--shaders":
                    options.ShaderDirs.Add(Path.GetFullPath(args[++i]));
                    break;
                case "--out":
                    options.OutDir = Path.GetFullPath(args[++i]);
                    break;
                case "--namespace":
                    options.Namespace = args[++i];
                    break;
                default:
                    throw new ToolException($"Unknown argument '{args[i]}'.");
            }
        }

        if (options.ShaderDirs.Count == 0 || options.OutDir.Length == 0)
            throw new ToolException("--shaders and --out are required.");

        return options;
    }
}
