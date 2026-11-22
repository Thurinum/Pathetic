using System.CommandLine;
using Pathetic.Slang.Tool;

Option<string[]> shaders = new("--shaders")
{
    Description = "Directory containing the .slang modules.",
    Required = true,
    Arity = ArgumentArity.OneOrMore,
    AllowMultipleArgumentsPerToken = true,
};

Option<string> output = new("--out")
{
    Description = "Directory receiving the manifests, SPIR-V blobs and generated C#.",
    Required = true,
};

Option<string> targetNamespace = new("--namespace")
{
    Description = "Namespace of the generated types.",
    DefaultValueFactory = _ => "Pathetic.Generated.Slang",
};

RootCommand root = new("Reflects Slang modules into C#, a runtime manifest and SPIR-V blobs.");
root.Options.Add(shaders);
root.Options.Add(output);
root.Options.Add(targetNamespace);

root.SetAction(parseResult =>
{
    try
    {
        ShaderSet shaderSet = ShaderSet.Discover(parseResult.GetValue(shaders)!.Select(Path.GetFullPath));
        string outDir = Path.GetFullPath(parseResult.GetValue(output)!);

        Directory.CreateDirectory(outDir);
        new Reflector(parseResult.GetValue(targetNamespace)!).Run(shaderSet, outDir);

        Console.WriteLine($"Pathetic.Slang.Tool: {shaderSet.Files.Count} module(s) written to '{outDir}'.");
        return 0;
    }
    catch (ToolException exception)
    {
        Console.Error.WriteLine($"error PATSLANG: {exception.Message}");
        return 1;
    }
});

return root.Parse(args).Invoke();
