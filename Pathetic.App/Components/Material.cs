using System.Runtime.InteropServices;
using NeoVeldrid;

namespace Pathetic.App.Components;

[StructLayout(LayoutKind.Sequential)]
public record struct Material
{
    public static readonly Material Default = new()
    {
        Albedo = new RgbaByte(128, 128, 128, 255),
        EmissionStrength = 1.0f
    };

    public RgbaByte Albedo;
    public RgbaByte Emission;
    public float EmissionStrength;
}
