namespace Pathetic.Input;

public interface IInputTrigger<in T> where T : struct
{
    bool CanTrigger(T value, float deltaTime);
    bool CanComplete(T value, float deltaTime) => true;
    void Reset();
}

public abstract class InputTrigger<T> : IInputTrigger<T> where T : struct
{
    protected float Elapsed { get; private set; }
    protected bool Fired { get; set; }
    protected void Advance(float deltaTime) => Elapsed += deltaTime;

    public abstract bool CanTrigger(T value, float deltaTime);
    public virtual bool CanComplete(T value, float deltaTime) => true;

    public void Reset()
    {
        Elapsed = 0f;
        Fired = false;
    }
}
