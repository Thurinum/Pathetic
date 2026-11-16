using NeoVeldrid;

namespace Pathetic.Profiling.Gpu;

public interface IGpuTimer
{
    void Lap(GraphicsDevice device, CommandList cmd, string label);
}
