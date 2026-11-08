namespace Pathetic.Input;

public interface IInputTrigger<in T> where T : struct
{
    bool CanTrigger(T value, float deltaTime);
    bool CanComplete(T value, float deltaTime) => true;
    void Reset();
}
