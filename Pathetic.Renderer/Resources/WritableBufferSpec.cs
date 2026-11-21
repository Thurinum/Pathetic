using System.Runtime.CompilerServices;
using NeoVeldrid;
using Pathetic.Renderer.Buffers;
using Pathetic.Renderer.Graph;

namespace Pathetic.Renderer.Resources;

public sealed class WritableBufferSpec<T>(string name, uint elementCount, GpuReadback<T> readback) : IStructuredBufferSpec
    where T : unmanaged
{
    public string Name => name;
    public uint Stride => (uint)Unsafe.SizeOf<T>();
    public uint ElementCount => elementCount;
    public uint InitialCapacity => elementCount;
    public bool IsResizable => false;
    public bool IsDynamic => false;
    public bool IsWritable => true;

    public IReadOnlyList<IResourceSpec> Resources => [this];
    public SceneChange DirtyChanges => SceneChange.None;

    public void SetDirty(SceneChange changes) { }
    public void ClearDirty() { }

    public void OnAllocated(DeviceBuffer buffer) => readback.Bind(buffer);

    public void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized) { }
}
