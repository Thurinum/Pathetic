using System.ComponentModel.DataAnnotations;
using Pathetic.Renderer;

namespace Pathetic.App.RenderPasses;

public sealed record TemporalAccumulationOptions : IConfig
{
    public static string Section => "TAA";
    
    public bool Enabled { get; set; }

    [Range(0, 1_000_000)]
    public uint SampleBudget { get; set; } = 1024;
}
