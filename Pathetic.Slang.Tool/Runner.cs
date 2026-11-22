namespace Pathetic.Slang.Tool;

internal static class Runner
{
    public static int Run(string[] args)
    {
        try
        {
            Options options = Options.Parse(args);
            Directory.CreateDirectory(options.OutDir);

            ShaderSet shaders = ShaderSet.Discover(options.ShaderDirs);
            Reflector reflector = new(options.Namespace);
            reflector.Run(shaders, options.OutDir);

            Console.WriteLine($"Pathetic.Slang.Tool: {shaders.Files.Count} module(s) written to '{options.OutDir}'.");
            return 0;
        }
        catch (ToolException exception)
        {
            Console.Error.WriteLine($"error PATSLANG: {exception.Message}");
            return 1;
        }
    }
}
