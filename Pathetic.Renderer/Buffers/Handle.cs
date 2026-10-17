namespace Pathetic.Renderer.Buffers;

public readonly record struct Handle(int Index, int Version)
{
    public static readonly Handle Invalid = new(-1, 0);
    public bool IsValid => Index >= 0;
}
