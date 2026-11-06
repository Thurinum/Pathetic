using System.Numerics;

namespace Pathetic.Input;

public interface IInputProcessor
{
    string Name { get; }
    Type InputType { get; }
    Type OutputType { get; }
    object Process(object value, float deltaTime);
}

public interface IInputProcessor<in TIn, out TOut> : IInputProcessor where TIn : struct where TOut : struct
{
    Type IInputProcessor.InputType => typeof(TIn);
    Type IInputProcessor.OutputType => typeof(TOut);
    object IInputProcessor.Process(object value, float deltaTime) => Process((TIn)value, deltaTime);
    TOut Process(TIn value, float deltaTime);
}

#region DEFAULT PROCESSORS

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

#endregion