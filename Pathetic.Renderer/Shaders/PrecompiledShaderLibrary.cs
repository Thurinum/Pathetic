using NeoVeldrid;
using Pathetic.Renderer.Utils;

namespace Pathetic.Renderer.Shaders;

public readonly record struct ShaderBinding(string Name, ResourceKind Kind, uint Slot, uint Space);
public readonly record struct CompiledShader(
    byte[] Spirv,
    UIntVector3 GroupSize,
    IReadOnlyList<ShaderBinding> Bindings);

public sealed class PrecompiledShaderLibrary
{
    public const string EntryPointName = "main";

    public CompiledShader LoadComputeShader(string moduleName)
    {
        ShaderModule module = ShaderModules.Get(moduleName);

        List<ShaderBinding> bindings = [];

        foreach (ShaderParameter parameter in module.Parameters)
        {
            bindings.Add(new ShaderBinding(
                parameter.Name,
                MapKind(parameter.Kind, moduleName),
                parameter.Slot,
                parameter.Space));
        }

        bindings.Sort(static (a, b) => a.Space != b.Space
            ? a.Space.CompareTo(b.Space)
            : a.Slot.CompareTo(b.Slot));

        return new CompiledShader(
            LoadSpirv(module),
            new UIntVector3(module.GroupSizeX, module.GroupSizeY, module.GroupSizeZ),
            bindings);
    }

    private static byte[] LoadSpirv(ShaderModule module)
    {
        using Stream stream = typeof(ShaderModule).Assembly.GetManifestResourceStream(module.SpirvResourceName)
            ?? throw new InvalidOperationException($"Shader module '{module.Name}' is missing its embedded SPIR-V resource '{module.SpirvResourceName}'.");

        using MemoryStream buffer = new();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static ResourceKind MapKind(ShaderResourceKind kind, string moduleName) => kind switch
    {
        ShaderResourceKind.Texture => ResourceKind.TextureReadOnly,
        ShaderResourceKind.MutableTexture => ResourceKind.TextureReadWrite,
        ShaderResourceKind.Sampler => ResourceKind.Sampler,
        ShaderResourceKind.UniformBuffer => ResourceKind.UniformBuffer,
        ShaderResourceKind.Buffer => ResourceKind.StructuredBufferReadOnly,
        ShaderResourceKind.MutableBuffer => ResourceKind.StructuredBufferReadWrite,
        _ => throw new InvalidOperationException($"Shader module '{moduleName}' uses unsupported binding kind '{kind}'."),
    };
}
