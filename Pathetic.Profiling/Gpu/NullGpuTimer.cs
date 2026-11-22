using NeoVeldrid;

namespace Pathetic.Profiling.Gpu;

public sealed class NullGpuTimer : IGpuTimer
{
    public void Record(GraphicsDevice device, CommandList cmd, string label)
    {
    }

    public void Publish(GraphicsDevice device, CommandList cmd)
    {
    }
}
