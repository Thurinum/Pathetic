using System.Runtime.InteropServices;
using Pathetic.Profiling.Gpu;
using Pathetic.Renderer.Shaders.Generator.Diagnostics;
using Pathetic.Renderer.Shaders.Generator.Models;
using Prowl.Slang;

namespace Pathetic.Renderer.Shaders.Generator.Compilation;

internal static class SlangShaderCompiler
{
    public static ShaderCompilationResult Compile(ShaderSourceSet sources, bool profileCounters)
    {
        Session session = CreateSession(sources.Directories, profileCounters);

        List<ModuleModel> modules = [];
        Dictionary<string, byte[]> spirvByModule = new(StringComparer.Ordinal);

        foreach (ShaderSource source in sources.Sources)
        {
            Module module = LoadModule(session, source);
            PipelineModel? pipeline = null;

            if (module.GetDefinedEntryPointCount() > 0)
            {
                (pipeline, byte[] spirv) = CompileEntryPoint(session, module);
                spirvByModule[source.ModuleName] = spirv;
            }

            modules.Add(SlangReflectionMapper.ReadModule(session, source, module, pipeline));
        }

        EnsureUniqueCpuTypes(modules);

        return new ShaderCompilationResult(modules, spirvByModule);
    }

    private static void EnsureUniqueCpuTypes(IEnumerable<ModuleModel> modules)
    {
        HashSet<string> names = new(StringComparer.Ordinal);

        foreach (TypeModel type in modules.SelectMany(static module => module.Types))
        {
            if (!names.Add(type.Name))
                throw new ShaderCompilerException($"Two [CPUType] declarations emit '{type.Name}'; give one an explicit name with [CPUType(\"...\")].");
        }
    }

    private static Session CreateSession(IReadOnlyList<string> searchPaths, bool profileCounters)
    {
        // Slang loads its SPIR-V optimizer itself, outside .NET's native library resolution.
        string nativeDirectory = Path.Combine(AppContext.BaseDirectory, "runtimes", RuntimeInformation.RuntimeIdentifier, "native");
        GlobalSession.SetDownstreamCompilerPath(PassThrough.SpirvOpt,
            Directory.Exists(nativeDirectory) ? nativeDirectory : AppContext.BaseDirectory);

        TargetDescription target = new()
        {
            Format = CompileTarget.Spirv,
            Profile = GlobalSession.FindProfile("spirv_1_5"),
        };

        SessionDescription description = new()
        {
            Targets = [target],
            SearchPaths = [.. searchPaths],
            PreprocessorMacros = profileCounters
                ? [.. ShaderCounterMacros.All.Select(static macro => new PreprocessorMacroDescription { Name = macro.Name, Value = macro.Value })]
                : null,
        };

        return GlobalSession.CreateSession(description);
    }

    private static Module LoadModule(Session session, ShaderSource source)
    {
        for (int i = 0; i < session.GetLoadedModuleCount(); i++)
        {
            Module loaded = session.GetLoadedModule(i);

            if (string.Equals(Path.GetFullPath(loaded.GetFilePath()), source.Path, StringComparison.OrdinalIgnoreCase))
                return loaded;
        }

        try
        {
            Module module = session.LoadModuleFromSourceString(source.ModuleName, source.Path, File.ReadAllText(source.Path), out DiagnosticInfo diagnostics);
            SlangDiagnostics.ThrowIfErrors(diagnostics);

            return module;
        }
        catch (CompilationException exception)
        {
            throw new SlangDiagnosticException(DiagnosticFormatter.Format(exception.Message));
        }
    }

    private static (PipelineModel Pipeline, byte[] Spirv) CompileEntryPoint(Session session, Module module)
    {
        EntryPoint entryPoint = module.GetDefinedEntryPoint(0);

        ComponentType composite = session.CreateCompositeComponentType([module, entryPoint], out DiagnosticInfo componentDiagnostics);
        SlangDiagnostics.ThrowIfErrors(componentDiagnostics);

        ComponentType linked = composite.Link(out DiagnosticInfo linkDiagnostics);
        SlangDiagnostics.ThrowIfErrors(linkDiagnostics);

        PipelineModel pipeline = SlangReflectionMapper.ReadPipeline(linked.GetLayout());

        byte[] spirv = linked.GetEntryPointCode(IntPtr.Zero, IntPtr.Zero, out DiagnosticInfo codeDiagnostics).ToArray();
        SlangDiagnostics.ThrowIfErrors(codeDiagnostics);

        return (pipeline, spirv);
    }
}
