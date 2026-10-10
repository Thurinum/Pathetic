namespace PathTracerCore;

public sealed class EngineOptions
{
    public string AppName = "Path Tracer";
    public uint WindowWidth;
    public uint WindowHeight;
    public int X;
    public int Y;
    
    public bool VSync = true;
    public string ShadersDir = "Shaders";
    
    public uint SamplesPerPixel = 4;
    public uint MaxBounces = 8;
}