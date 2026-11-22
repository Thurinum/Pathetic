namespace Pathetic.Profiling.Gpu;

public static class ShaderCounterMacros
{
    private const string EnabledName = "PROFILE_COUNTERS";
    private const string CountName = "PROFILE_COUNTER_COUNT";
    private const string CounterPrefix = "PROFILE_";

    public static IEnumerable<(string Name, string Value)> All =>
    [
        (EnabledName, "1"),
        (CountName, $"{Enum.GetValues<ShaderCounter>().Length}"),
        .. Enum.GetValues<ShaderCounter>().Select(counter => (CounterPrefix + counter, $"{(int)counter}")),
    ];
}
