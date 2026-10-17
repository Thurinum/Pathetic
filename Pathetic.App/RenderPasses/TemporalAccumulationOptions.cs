using Pathetic.Renderer;

namespace Pathetic.App.RenderPasses;

public sealed record TemporalAccumulationOptions : IConfig
{
    public static string Section => "TAA";
    
    public bool Enabled { get; set; }
}