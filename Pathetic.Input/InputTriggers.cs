namespace Pathetic.Input;

/// <summary>
/// Fires once per press instead of every frame the action stays above its threshold.
/// </summary>
public sealed class Press<T> : InputTrigger<T> where T : struct
{
    public override bool CanTrigger(T value, float deltaTime)
    {
        if (Fired)
            return false;

        Fired = true;
        return true;
    }

    // Do not fire again when the action is released; Completed still reports the release.
    public override bool CanComplete(T value, float deltaTime) => false;
}

/// <summary>
/// Fires once after the action stayed above its threshold for <paramref name="duration"/> seconds.
/// Releasing earlier completes the action without triggering it.
/// </summary>
public sealed class Hold<T>(float duration) : InputTrigger<T> where T : struct
{
    public override bool CanTrigger(T value, float deltaTime)
    {
        if (Fired)
            return false;

        Advance(deltaTime);

        if (Elapsed < duration)
            return false;

        Fired = true;
        return true;
    }

    public override bool CanComplete(T value, float deltaTime) => false;
}

/// <summary>
/// Fires when the action drops below its threshold, i.e. on release. Pass <paramref name="maxHoldDuration"/>
/// to only accept short presses (a tap); the default accepts a release after any duration.
/// </summary>
public sealed class Release<T>(float maxHoldDuration = float.PositiveInfinity) : InputTrigger<T> where T : struct
{
    public override bool CanTrigger(T value, float deltaTime)
    {
        Advance(deltaTime);
        return false;
    }

    public override bool CanComplete(T value, float deltaTime) => Elapsed <= maxHoldDuration;
}
