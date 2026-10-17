namespace Pathetic.Renderer.Resources;

public abstract record TextureSizePolicy;
public sealed record AutoSize : TextureSizePolicy;
public sealed record ExplicitSize(uint Width, uint Height) : TextureSizePolicy;