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