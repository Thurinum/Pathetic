using NeoVeldrid;

namespace Pathetic.App.Scenes;

public static class ColorExtensions
{
    private const float ChannelMax = 255.0f;

    extension(RgbaByte)
    {
        public static RgbaByte Rgb(float r, float g, float b) =>
            new((byte)MathF.Round(r * ChannelMax), (byte)MathF.Round(g * ChannelMax), (byte)MathF.Round(b * ChannelMax), 255);
    }
}
