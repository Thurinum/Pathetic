namespace Pathetic.Renderer.Utils;

public readonly record struct UIntVector2(uint X, uint Y)
{
    public static implicit operator UIntVector2((uint x, uint y) value) => new(value.x, value.y);
}

public readonly record struct UIntVector3(uint X, uint Y, uint Z)
{
    public static implicit operator UIntVector3((uint x, uint y, uint z) value) => new(value.x, value.y, value.z);
}