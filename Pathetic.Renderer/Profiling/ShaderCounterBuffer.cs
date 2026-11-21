using NeoVeldrid;
using Pathetic.Profiling.Gpu;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.Renderer.Profiling;

public sealed class ShaderCounterBuffer : IResourceUploader, IShaderCounterReadback
{
    public const string ResourceName = "profileCounters";

    private readonly uint _counterCount;
    private readonly uint _sizeInBytes;

    private DeviceBuffer? _counters;
    private DeviceBuffer? _staging;

    public ShaderCounterBuffer(IShaderCounterLayout layout)
    {
        _counterCount = (uint)layout.Count;
        _sizeInBytes = Math.Max(1u, _counterCount) * sizeof(uint);
        Resources = [new RwBufferSpec(ResourceName, _sizeInBytes, sizeof(uint))];
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

    public uint[] Resolve(GraphicsDevice device)
    {
        if (_counters is null || _staging is null)
            return [];

        MappedResourceView<uint> view = device.Map<uint>(_staging, MapMode.Read);
        uint[] values = [.. Enumerable.Range(0, (int)_counterCount).Select(index => view[index])];
        device.Unmap(_staging);

        if (values.All(value => value == 0))
            return [];

        device.UpdateBuffer(_counters, 0, new uint[_counterCount]);
        return values;
    }
}
