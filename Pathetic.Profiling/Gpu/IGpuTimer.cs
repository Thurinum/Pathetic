using NeoVeldrid;

namespace Pathetic.Profiling.Gpu;

public interface IGpuTimer
{
    /// <summary>Marks the end of a GPU workload without waiting for it.</summary>
    void Record(GraphicsDevice device, CommandList cmd, string label);

    /// <summary>Waits for everything recorded this frame and reports the results.</summary>
    void Publish(GraphicsDevice device, CommandList cmd);
}
