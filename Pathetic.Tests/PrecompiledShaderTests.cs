using Microsoft.Extensions.Options;
using NeoVeldrid;
using Pathetic.Renderer;
using Pathetic.Renderer.Shaders;
using Xunit;

namespace Pathetic.Tests;

public sealed class PrecompiledShaderTests
{
    [Fact]
    public void LoadsPathTracerPipelineFromCompiledAssets()
    {
        CopyCompiledShaders();

        PrecompiledShaderCache compiler = CreateCompiler();
        ShaderCompilationResult result = compiler.CompileComputeShader("PathTracer");

        Assert.NotEmpty(result.Code);
        Assert.Equal(8u, result.GroupSize.X);
        Assert.Equal(8u, result.GroupSize.Y);
        Assert.Equal(1u, result.GroupSize.Z);
        Assert.Equal(11, result.Reflection.Count);
        Assert.Contains(result.Reflection, binding => binding.Name == "camera" && binding.Kind == ResourceKind.UniformBuffer);
        Assert.Contains(result.Reflection, binding => binding.Name == "spheres" && binding.Kind == ResourceKind.StructuredBufferReadOnly);
        Assert.Contains(result.Reflection, binding => binding.Name == "pathTracerColor" && binding.Kind == ResourceKind.TextureReadWrite);
    }

    [Fact]
    public void LoadsCirclesPipelineWithUniformBuffer()
    {
        CopyCompiledShaders();

        PrecompiledShaderCache compiler = CreateCompiler();
        ShaderCompilationResult result = compiler.CompileComputeShader("Circles");

        ShaderBinding parameters = Assert.Single(result.Reflection, binding => binding.Name == "params");
        Assert.Equal(ResourceKind.UniformBuffer, parameters.Kind);
        Assert.Equal(2u, parameters.BindingIndex);
    }

    [Fact]
    public void ThrowsForUnknownModule()
    {
        CopyCompiledShaders();

        PrecompiledShaderCache compiler = CreateCompiler();

        Assert.Throws<InvalidOperationException>(() => compiler.CompileComputeShader("DoesNotExist"));
    }

    private static PrecompiledShaderCache CreateCompiler() =>
        new(Options.Create(new RendererOptions { ShadersDir = "Shaders" }));

    private static void CopyCompiledShaders()
    {
        string? root = AppContext.BaseDirectory;

        while (root is not null && !File.Exists(Path.Combine(root, "Pathetic.slnx")))
            root = Path.GetDirectoryName(root);

        Assert.NotNull(root);

        string source = Path.Combine(root, "Pathetic.App", "obj", "slang");
        string destination = Path.Combine(AppContext.BaseDirectory, "Shaders", "Compiled");

        Assert.True(Directory.Exists(source), $"Compiled shader directory '{source}' does not exist. Build Pathetic.App first.");

        Directory.CreateDirectory(destination);

        foreach (string file in Directory.EnumerateFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
    }
}
