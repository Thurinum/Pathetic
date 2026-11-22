using System.Text.Json;
using Microsoft.Extensions.Options;
using NeoVeldrid;
using Pathetic.Renderer.Utils;

namespace Pathetic.Renderer.Shaders;

public readonly record struct ShaderBinding(string Name, ResourceKind Kind, uint BindingIndex, uint BindingSpace);
public readonly record struct ShaderCompilationResult(
    byte[] Code,
    UIntVector3 GroupSize,
    IReadOnlyList<ShaderBinding> Reflection);

public sealed class PrecompiledShaderLibrary
{
    public const string EntryPointName = "main";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly RendererOptions _options;
    private readonly Lazy<IReadOnlyDictionary<string, string>> _files;

    public PrecompiledShaderLibrary(IOptions<RendererOptions> options)
    {
        _options = options.Value;
        _files = new Lazy<IReadOnlyDictionary<string, string>>(LoadIndex);
    }

    public ShaderCompilationResult LoadComputeShader(string moduleName)
    {
        string directory = CompiledDirectory;

        if (!_files.Value.TryGetValue(moduleName, out string? file))
            throw new InvalidOperationException($"Shader module '{moduleName}' has no precompiled manifest.");

        string manifestPath = Path.Combine(directory, file + ".json");
        ModuleManifest manifest = JsonSerializer.Deserialize<ModuleManifest>(File.ReadAllText(manifestPath), JsonOptions)
            ?? throw new InvalidOperationException($"Shader manifest '{manifestPath}' is empty.");

        PipelineManifest pipeline = manifest.Pipeline
            ?? throw new InvalidOperationException($"Shader module '{moduleName}' has no precompiled compute pipeline.");

        string codePath = Path.Combine(directory, file + ".spv");

        if (!File.Exists(codePath))
            throw new InvalidOperationException($"Shader module '{moduleName}' is missing its SPIR-V blob '{codePath}'.");

        List<ShaderBinding> bindings = [];

        foreach (ParameterManifest parameter in pipeline.Parameters)
        {
            bindings.Add(new ShaderBinding(
                parameter.Name,
                MapKind(parameter.Kind, moduleName),
                parameter.Binding,
                parameter.Space));
        }

        bindings.Sort(static (a, b) => a.BindingSpace != b.BindingSpace
            ? a.BindingSpace.CompareTo(b.BindingSpace)
            : a.BindingIndex.CompareTo(b.BindingIndex));

        uint[] group = pipeline.ThreadGroupSize;

        if (group.Length != 3)
            throw new InvalidOperationException($"Shader module '{moduleName}' has an invalid thread group size.");

        return new ShaderCompilationResult(
            File.ReadAllBytes(codePath),
            new UIntVector3(group[0], group[1], group[2]),
            bindings);
    }

    private string CompiledDirectory => Path.Combine(AppContext.BaseDirectory, _options.ShadersDir, "Compiled");

    private Dictionary<string, string> LoadIndex()
    {
        string path = Path.Combine(CompiledDirectory, "index.json");

        if (!File.Exists(path))
            throw new InvalidOperationException($"Precompiled shader index '{path}' does not exist.");

        IndexManifest index = JsonSerializer.Deserialize<IndexManifest>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidOperationException($"Precompiled shader index '{path}' is empty.");

        Dictionary<string, string> files = new(StringComparer.Ordinal);

        foreach (IndexEntry entry in index.Modules)
            files[entry.Module] = entry.File;

        return files;
    }

    private static ResourceKind MapKind(string kind, string moduleName) => kind switch
    {
        "MutableTexture" => ResourceKind.TextureReadWrite,
        "Texture" => ResourceKind.TextureReadOnly,
        "Sampler" => ResourceKind.Sampler,
        "UniformBuffer" => ResourceKind.UniformBuffer,
        "BufferRead" => ResourceKind.StructuredBufferReadOnly,
        "BufferReadWrite" => ResourceKind.StructuredBufferReadWrite,
        _ => throw new InvalidOperationException($"Shader module '{moduleName}' uses unsupported binding kind '{kind}'."),
    };

    private sealed class IndexManifest
    {
        public List<IndexEntry> Modules { get; set; } = [];
    }

    private sealed class IndexEntry
    {
        public string Module { get; set; } = "";
        public string File { get; set; } = "";
    }

    private sealed class ModuleManifest
    {
        public PipelineManifest? Pipeline { get; set; }
    }

    private sealed class PipelineManifest
    {
        public uint[] ThreadGroupSize { get; set; } = [];
        public List<ParameterManifest> Parameters { get; set; } = [];
    }

    private sealed class ParameterManifest
    {
        public string Name { get; set; } = "";
        public string Kind { get; set; } = "";
        public uint Space { get; set; }
        public uint Binding { get; set; }
    }
}
