using Pathetic.Renderer;

namespace Pathetic.App.RenderPasses;

public enum Tonemapper : uint
{
    None = 0,
    Reinhard = 1,
    Aces = 2,
    Agx = 3,
}

public sealed record TonemappingOptions : IConfig
{
    public static string Section => "Tonemapping";

    public Tonemapper Tonemapper { get; set; } = Tonemapper.Aces;
    public float Exposure { get; set; } = 0;
}
