using System.Runtime.InteropServices;
using NeoVeldrid;
using Pathetic.Profiling.Gpu;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.Renderer.Profiling;

public sealed class ShaderCounterBuffer : IResourceUploader, IShaderCounterReadback
{
    public const string ResourceName = "profileCounters";

    private readonly uint _sizeInBytes;

    private DeviceBuffer? _counters;
    private DeviceBuffer? _staging;

    public ShaderCounterBuffer(IShaderCounterSource source)
    {
        _sizeInBytes = source.SizeInBytes;
        Resources = [new RwBufferSpec(ResourceName, _sizeInBytes, _sizeInBytes)];
    }

    public IReadOnlyList<IResourceSpec> Resources { get; }

    public SceneChange DirtyChanges { get; private set; } = SceneChange.All;

    public void SetDirty(SceneChange changes) => DirtyChanges |= changes;

    public void ClearDirty() => DirtyChanges = SceneChange.None;

    public void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized) => _counters = buffer;

    public void Record(GraphicsDevice device, CommandList cmd)
    {
        if (_counters is null)
            return;

        _staging ??= device.ResourceFactory.CreateBuffer(new BufferDescription(_sizeInBytes, BufferUsage.Staging));
        cmd.CopyBuffer(_counters, 0, _staging, 0, _sizeInBytes);
    }

    public byte[] Resolve(GraphicsDevice device)
    {
        if (_counters is null || _staging is null)
            return [];

        MappedResource mapped = device.Map(_staging, MapMode.Read);
        byte[] values = new byte[_sizeInBytes];
        Marshal.Copy(mapped.Data, values, 0, values.Length);
        device.Unmap(_staging);

        if (values.All(value => value == 0))
            return [];

        device.UpdateBuffer(_counters, 0, new byte[_sizeInBytes]);
        return values;
    }
}
