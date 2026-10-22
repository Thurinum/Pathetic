using System.ComponentModel.DataAnnotations;

namespace Pathetic.Renderer;

public sealed record RendererOptions : IConfig
{
    public static string Section => "Renderer";

    public string WindowTitle { get; init; } = "Path Tracer";
    
    [Range(1, uint.MaxValue)]
    public uint WindowWidth { get; init; } = 1920;
    
    [Range(1, uint.MaxValue)]
    public uint WindowHeight { get; init; } = 1080;
    
    public int X { get; init; }
    
    public int Y { get; init; }
    
    public bool VSync { get; init; }
    
    public string ShadersDir { get; init; } = "Shaders";
}