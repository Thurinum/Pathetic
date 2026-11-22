using System.CommandLine;
using Pathetic.Renderer.Shaders.Generator;

Option<string[]> shaders = new("--shaders")
{
    Description = "Directory containing the .slang modules.",
    Required = true,
    Arity = ArgumentArity.OneOrMore,
    AllowMultipleArgumentsPerToken = true,
};

Option<string> output = new("--out")
{
    Description = "Directory receiving the SPIR-V blobs and generated C#.",
    Required = true,
};

Option<string[]> search = new("--search")
{
    Description = "Additional directories used to resolve imports.",
    Arity = ArgumentArity.OneOrMore,
    AllowMultipleArgumentsPerToken = true,
};

Option<string> targetNamespace = new("--namespace")
{
    Description = "Namespace of the generated types.",
    DefaultValueFactory = _ => "Pathetic.Renderer.Shaders.GPUTypes",
};

RootCommand root = new("Reflects Slang modules into C# and SPIR-V blobs.");
root.Options.Add(shaders);
root.Options.Add(search);
root.Options.Add(output);
root.Options.Add(targetNamespace);

root.SetAction(parseResult =>
{
    try
    {
        ShaderSet shaderSet = ShaderSet.Discover(
            parseResult.GetValue(shaders)!.Select(Path.GetFullPath),
            (parseResult.GetValue(search) ?? []).Select(Path.GetFullPath));
        string outDir = Path.GetFullPath(parseResult.GetValue(output)!);

        Directory.CreateDirectory(outDir);
        new Reflector(parseResult.GetValue(targetNamespace)!).Run(shaderSet, outDir);

        Console.WriteLine($"Pathetic.Renderer.Shaders.Generator: {shaderSet.Files.Count} module(s) written to '{outDir}'.");
        return 0;
    }
    catch (ToolException exception)
    {
        Console.Error.WriteLine($"error PATSLANG: {exception.Message}");
        return 1;
    }
});

return root.Parse(args).Invoke();
