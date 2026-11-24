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

Option<bool> profileCounters = new("--profile-counters")
{
    Description = "Defines the profiler's shader counter macros.",
};

RootCommand root = new("Reflects Slang modules into C# and SPIR-V blobs.");
root.Options.Add(shaderDirectories);
root.Options.Add(output);
root.Options.Add(profileCounters);

root.SetAction(parseResult =>
{
    try
    {
        ShaderGenerationPipeline.Run(new ShaderGenerationOptions(
            [.. parseResult.GetValue(shaderDirectories)!.Select(Path.GetFullPath)],
            Path.GetFullPath(parseResult.GetValue(output)!),
            parseResult.GetValue(profileCounters)));

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
