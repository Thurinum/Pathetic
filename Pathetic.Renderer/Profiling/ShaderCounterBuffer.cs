using NeoVeldrid;
using Pathetic.Profiling.Gpu;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.Renderer.Profiling;

public sealed class ShaderCounterBuffer : IResourceUploader, IShaderCounterReadback
{
    public const string ResourceName = "profileCounters";

    private static readonly uint CounterCount = (uint)Enum.GetValues<ShaderCounter>().Length;
    private static readonly uint SizeInBytes = CounterCount * sizeof(uint);

    private DeviceBuffer? _counters;
    private DeviceBuffer? _staging;

    public IReadOnlyList<IResourceSpec> Resources { get; } = [new RwBufferSpec(ResourceName, SizeInBytes, sizeof(uint))];

    public SceneChange DirtyChanges { get; private set; } = SceneChange.All;

    public void SetDirty(SceneChange changes) => DirtyChanges |= changes;

    public void ClearDirty() => DirtyChanges = SceneChange.None;

    public void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized) => _counters = buffer;

    public void Record(GraphicsDevice device, CommandList cmd)
    {
        if (_counters is null)
            return;

        _staging ??= device.ResourceFactory.CreateBuffer(new BufferDescription(SizeInBytes, BufferUsage.Staging));
        cmd.CopyBuffer(_counters, 0, _staging, 0, SizeInBytes);
    }

    public uint[] Resolve(GraphicsDevice device)
    {
        if (_counters is null || _staging is null)
            return [];

        MappedResourceView<uint> view = device.Map<uint>(_staging, MapMode.Read);
        uint[] values = [.. Enumerable.Range(0, (int)CounterCount).Select(index => view[index])];
        device.Unmap(_staging);

        if (values.All(value => value == 0))
            return [];

        device.UpdateBuffer(_counters, 0, new uint[CounterCount]);
        return values;
    }
}
