using System.Runtime.InteropServices;
using NeoVeldrid;
using Pathetic.Renderer.Shaders;

namespace Pathetic.App.Blender;

/// <summary>
/// Reusable GPU-to-CPU copy of the present texture. The bridge keeps one of these
/// alive so interactive frames do not recompile shaders or reallocate buffers.
/// </summary>
public sealed class ReadbackCapture : IDisposable
{
    private readonly DeviceBuffer _storage;
    private readonly DeviceBuffer _staging;
    private readonly Shader _shader;
    private readonly ResourceLayout _layout;
    private readonly Pipeline _pipeline;
    private readonly ResourceSet _resourceSet;
    private readonly uint _byteSize;
    private bool _disposed;

    public ReadbackCapture(GraphicsDevice device, Texture presentTexture, SlangCompiler compiler)
    {
        uint width = presentTexture.Width;
        uint height = presentTexture.Height;
        _byteSize = checked(width * height * 4);

        ResourceFactory factory = device.ResourceFactory;
        _storage = factory.CreateBuffer(new BufferDescription(_byteSize, BufferUsage.StructuredBufferReadWrite, 4));
        _staging = factory.CreateBuffer(new BufferDescription(_byteSize, BufferUsage.Staging));

        ShaderCompilationResult compiled = compiler.CompileComputeShader("Readback");
        _shader = factory.CreateShader(
            new ShaderDescription(ShaderStages.Compute, compiled.Code, SlangCompiler.EntryPointName));

        ResourceLayoutElementDescription[] bindings = compiled.Reflection
            .Select(binding => new ResourceLayoutElementDescription(binding.Name, binding.Kind, ShaderStages.Compute))
            .ToArray();

        _layout = factory.CreateResourceLayout(new ResourceLayoutDescription(bindings));
        _pipeline = factory.CreateComputePipeline(new ComputePipelineDescription(
            _shader,
            _layout,
            compiled.GroupSize.X,
            compiled.GroupSize.Y,
            compiled.GroupSize.Z));

        _resourceSet = factory.CreateResourceSet(new ResourceSetDescription(
            _layout,
            presentTexture,
            _storage));
    }

    public byte[] Capture(GraphicsDevice device, CommandList commandList, uint width, uint height)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        commandList.Begin();
        commandList.SetPipeline(_pipeline);
        commandList.SetComputeResourceSet(0, _resourceSet);
        commandList.Dispatch(
            (width + 7) / 8,
            (height + 7) / 8,
            1);
        commandList.CopyBuffer(_storage, 0, _staging, 0, _byteSize);
        commandList.End();
        device.SubmitCommands(commandList);
        device.WaitForIdle();

        MappedResource mapped = device.Map(_staging, MapMode.Read);
        byte[] rgba = new byte[_byteSize];

        try
        {
            Marshal.Copy(mapped.Data, rgba, 0, (int)_byteSize);
        }
        finally
        {
            device.Unmap(_staging);
        }

        return rgba;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _resourceSet.Dispose();
        _pipeline.Dispose();
        _layout.Dispose();
        _shader.Dispose();
        _staging.Dispose();
        _storage.Dispose();
    }
}
