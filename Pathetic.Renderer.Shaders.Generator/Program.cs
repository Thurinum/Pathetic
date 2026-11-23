using System.CommandLine;
using Pathetic.Renderer.Shaders.Generator;
using Pathetic.Renderer.Shaders.Generator.Diagnostics;
using Prowl.Slang;

Option<string[]> shaderDirectories = new("--shader-dirs")
{
    Description = "Directories containing the .slang modules.",
    Required = true,
    Arity = ArgumentArity.OneOrMore,
    AllowMultipleArgumentsPerToken = true,
};

Option<string> output = new("--out")
{
    Description = "Directory receiving the SPIR-V blobs and generated C#.",
    Required = true,
};

RootCommand root = new("Reflects Slang modules into C# and SPIR-V blobs.");
root.Options.Add(shaderDirectories);
root.Options.Add(output);

root.SetAction(parseResult =>
{
    try
    {
        ShaderSet shaderSet = ShaderSet.Discover([.. parseResult.GetValue(shaderDirectories)!.Select(Path.GetFullPath)]);
        string outDir = Path.GetFullPath(parseResult.GetValue(output)!);

        new ShaderCompiler().Compile(shaderSet, outDir);

        Console.WriteLine($"Pathetic.Renderer.Shaders.Generator: {shaderSet.Files.Count} module(s) written to '{outDir}'.");
        return 0;
    }
    catch (SlangDiagnosticException exception)
    {
        Console.Error.WriteLine(exception.Message);
        return 1;
    }
    catch (CompilationException exception)
    {
        Console.Error.WriteLine(DiagnosticFormatter.Format(exception.Message));
        return 1;
    }
    catch (ShaderCompilerException exception)
    {
        Console.Error.WriteLine($"Pathetic Slang Interop error: {exception.Message}");
        return 1;
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine($"Pathetic Slang interop internal error: {exception.GetType().Name}: {exception.Message}");

        if (exception.StackTrace is { Length: > 0 } stackTrace)
            Console.Error.WriteLine(stackTrace);

        return 1;
    }
});

return root.Parse(args).Invoke();
