using Microsoft.Extensions.Options;
using NeoVeldrid;
using Pathetic.Renderer.Utils;
using Prowl.Slang;

namespace Pathetic.Renderer.Shaders;

public readonly record struct ShaderBinding(string Name, ResourceKind Kind, uint BindingIndex, uint BindingSpace);
public readonly record struct ShaderCompilationResult(
    byte[] Code,
    uint3 GroupSize,
    IReadOnlyList<ShaderBinding> Reflection);

public sealed class SlangCompiler
{
    private readonly RendererOptions _options;
    private readonly Session _session;
    public const string EntryPointName = "main";

    public SlangCompiler(IOptions<RendererOptions> options)
    {
        _options = options.Value;
        
        TargetDescription target = new()
        {
            Format = CompileTarget.Spirv,
            Profile = GlobalSession.FindProfile("spirv_1_5")
        };

        string shadersDirectory = Path.Combine(AppContext.BaseDirectory, _options.ShadersDir);
        SessionDescription sessionDescription = new()
        {
            Targets = [target],
            SearchPaths = [shadersDirectory, "./"]
        };

        _session = GlobalSession.CreateSession(sessionDescription);
    }
    
    public ShaderCompilationResult CompileComputeShader(string moduleName)
    {
        Module module = _session.LoadModule(moduleName, out DiagnosticInfo loadDiagnostics);

        ThrowIfErrors(loadDiagnostics);

        EntryPoint entryPoint = module.FindAndCheckEntryPoint(
            EntryPointName,
            ShaderStage.Compute,
            out var entryPointDiagnostics);

        ThrowIfErrors(entryPointDiagnostics);

        ComponentType program = _session.CreateCompositeComponentType([module, entryPoint], out var componentDiagnostics);

        ThrowIfErrors(componentDiagnostics);

        ComponentType linked = program.Link(out var linkDiagnostics);

        ThrowIfErrors(linkDiagnostics);

        Memory<byte> code = linked.GetEntryPointCode(
            IntPtr.Zero,
            IntPtr.Zero,
            out DiagnosticInfo codeDiagnostics);

        ThrowIfErrors(codeDiagnostics);

        ShaderReflection layout = linked.GetLayout();
        EntryPointReflection entry = layout.FindEntryPointByName(EntryPointName);
        var groupSize = entry.GetComputeThreadGroupSize();

        List<ShaderBinding> resources = [];
        for (uint i = 0; i < layout.ParameterCount; i++)
        {
            VariableLayoutReflection parameter = layout.GetParameterByIndex(i);
            if (parameter.Category != ParameterCategory.DescriptorTableSlot)
                continue;

            BindingType bindingType = parameter.TypeLayout.GetBindingRangeType(0);
            resources.Add(new ShaderBinding(
                parameter.Name,
                MapBindingType(bindingType),
                parameter.BindingIndex,
                parameter.BindingSpace));
        }

        resources.Sort(static (a, b) => a.BindingSpace != b.BindingSpace
            ? a.BindingSpace.CompareTo(b.BindingSpace)
            : a.BindingIndex.CompareTo(b.BindingIndex));

        return new ShaderCompilationResult(code.ToArray(), groupSize, resources);
    }

    private static ResourceKind MapBindingType(BindingType type) => type switch
    {
        BindingType.MutableTexture => ResourceKind.TextureReadWrite,
        BindingType.Texture => ResourceKind.TextureReadOnly,
        BindingType.Sampler => ResourceKind.Sampler,
        BindingType.ConstantBuffer => ResourceKind.UniformBuffer,
        BindingType.TypedBuffer or BindingType.RawBuffer => ResourceKind.StructuredBufferReadOnly,
        BindingType.MutableTypedBuffer or BindingType.MutableRawBuffer => ResourceKind.StructuredBufferReadWrite,
        _ => throw new NotSupportedException($"Unsupported shader binding type '{type}'.")
    };

    private static void ThrowIfErrors(DiagnosticInfo diagnostics)
    {
        List<string> errors = diagnostics.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity >= Severity.Error)
            .Select(diagnostic => diagnostic.Message)
            .ToList();

        if (errors.Count > 0)
        {
            throw new InvalidOperationException("Slang compilation failed: " + string.Join(Environment.NewLine, errors));
        }
    }
}
