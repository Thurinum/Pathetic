using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NeoVeldrid;

namespace Pathetic.Renderer.Buffers;

public sealed class GpuReadback<T>(RenderWindow window) : IDisposable where T : unmanaged
{
    private static readonly uint SizeInBytes = (uint)Unsafe.SizeOf<T>();

    private DeviceBuffer? _source;
    private DeviceBuffer? _staging;
    private bool _pending;
    private bool _disposed;
    
    public DeviceBuffer Source => _source ?? throw new InvalidOperationException($"{nameof(GpuReadback<>)} has no source buffer; call {nameof(Bind)} first.");
    
    public void Bind(DeviceBuffer source)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (source.SizeInBytes < SizeInBytes)
            throw new ArgumentException($"{source.SizeInBytes} bytes is too small for a {typeof(T).Name}.", nameof(source));

        _source = source;
    }
    
    public void RecordCopy(CommandList cmd)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        DeviceBuffer staging = EnsureStaging();
        cmd.CopyBuffer(Source, 0, staging, 0, SizeInBytes);
        _pending = true;
    }
    
    public void Wait()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_pending)
            return;

        window.Device.WaitForIdle();
    }
    
    public bool TryRead(out T value)
    {
        value = default;

        if (!_pending || _staging is null || _disposed)
            return false;

        GraphicsDevice device = window.Device;
        MappedResource mapped = device.Map(_staging, MapMode.Read);
        try
        {
            value = Marshal.PtrToStructure<T>(mapped.Data);
            _pending = false;
            return true;
        }
        finally
        {
            device.Unmap(_staging);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _staging?.Dispose();
        _staging = null;
    }
    
    private DeviceBuffer EnsureStaging()
    {
        return _staging ??= window.Device.ResourceFactory.CreateBuffer(new BufferDescription(SizeInBytes, BufferUsage.Staging));
    }
}
