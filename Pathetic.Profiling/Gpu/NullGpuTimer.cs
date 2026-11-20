using NeoVeldrid;

namespace Pathetic.Profiling.Gpu;

public sealed class NullGpuTimer : IGpuTimer
{
    public void Lap(GraphicsDevice device, CommandList cmd, string label)
    {
    }
}
