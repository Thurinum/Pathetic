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

    public ShaderCompilationResult LoadComputeShader(string moduleName)
    {
        ShaderModule module = ShaderModules.Get(moduleName);

        List<ShaderBinding> bindings = [];

        foreach (ShaderParameter parameter in module.Parameters)
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

        return new ShaderCompilationResult(
            module.LoadCode(),
            new UIntVector3(module.GroupSizeX, module.GroupSizeY, module.GroupSizeZ),
            bindings);
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
