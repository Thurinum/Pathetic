using NeoVeldrid;

namespace Pathetic.Profiling.Gpu;

public interface IShaderCounterReadback
{
    void Record(GraphicsDevice device, CommandList cmd);

    byte[] Resolve(GraphicsDevice device);
}
