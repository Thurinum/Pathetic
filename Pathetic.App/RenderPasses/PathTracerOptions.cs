using System.ComponentModel.DataAnnotations;
using Pathetic.Renderer;

namespace Pathetic.App.RenderPasses;

public sealed record PathTracerOptions : IConfig
{
    public static string Section => "PathTracer";
    
    [Range(1, 4096)] 
    public uint SamplesPerPixel { get; set; } = 4;
    
    [Range(1, 67)]
    public uint MaxBounces { get; set; } = 8;
}