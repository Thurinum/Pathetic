using NeoVeldrid;

namespace Pathetic.Profiling.Gpu;

public interface IShaderCounterReadback
{
    void Record(GraphicsDevice device, CommandList cmd);

    uint[] Resolve(GraphicsDevice device);
}
