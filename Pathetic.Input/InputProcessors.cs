using System.Numerics;

namespace Pathetic.Input;

public sealed class ScaleFloat(float factor) : IInputProcessor<float, float>
{
    public string Name => $"Scale({factor})";
    public float Process(float value, float deltaTime) => value * factor;
}

public sealed class ScaleVector2(float factor) : IInputProcessor<Vector2, Vector2>
{
    public string Name => $"Scale({factor})";
    public Vector2 Process(Vector2 value, float deltaTime) => value * factor;
}

public sealed class RateFloat(float perSecond) : IInputProcessor<float, float>
{
    public string Name => $"Rate({perSecond}/s)";
    public float Process(float value, float deltaTime) => value * perSecond * deltaTime;
}

public sealed class RateVector2(float perSecond) : IInputProcessor<Vector2, Vector2>
{
    public string Name => $"Rate({perSecond}/s)";
    public Vector2 Process(Vector2 value, float deltaTime) => value * perSecond * deltaTime;
}

public sealed class SwizzleY : IInputProcessor<Vector2, float>
{
    public string Name => "SwizzleY";
    public float Process(Vector2 value, float deltaTime) => value.Y;
}

public sealed class SwizzleX : IInputProcessor<Vector2, float>
{
    public string Name => "SwizzleX";
    public float Process(Vector2 value, float deltaTime) => value.X;
}

public sealed class SwizzleVector2 : IInputProcessor<Vector2, Vector2>
{
    public string Name => "Swizzle(YX)";
    public Vector2 Process(Vector2 value, float deltaTime) => new(value.Y, value.X);
}

public sealed class Negate<T> : IInputProcessor<T, T> where T : struct
{
    static Negate()
    {
        if (typeof(T) != typeof(float) && typeof(T) != typeof(Vector2) && typeof(T) != typeof(Vector3))
            throw new NotSupportedException($"{typeof(T)} cannot be negated. Must be one of float, Vector2, Vector3.");
    }

    public string Name => "Negate";

    public T Process(T value, float deltaTime) => value switch
    {
        float f => (T)(object)-f,
        Vector2 v => (T)(object)-v,
        Vector3 v => (T)(object)-v,
        _ => throw new NotSupportedException($"{typeof(T)} cannot be negated.")
    };
}

public sealed class ButtonToVector2(Vector2 direction) : IInputProcessor<bool, Vector2>
{
    public string Name => $"ButtonToVector2({direction.X}, {direction.Y})";
    public Vector2 Process(bool value, float deltaTime) => value ? direction : Vector2.Zero;
}

public sealed class DeadzoneVector2(float min, float max = 1f) : IInputProcessor<Vector2, Vector2>
{
    public string Name => $"Deadzone({min}, {max})";

    public Vector2 Process(Vector2 value, float deltaTime)
    {
        float magnitude = value.Length();

        if (magnitude <= min)
            return Vector2.Zero;

        float scale = MathF.Min((magnitude - min) / (max - min), 1f);
        return value * (scale / magnitude);
    }
}