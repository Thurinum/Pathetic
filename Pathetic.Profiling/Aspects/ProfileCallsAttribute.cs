using System.Diagnostics;

namespace Pathetic.Profiling.Aspects;

[Conditional(ProfilingName.Symbol)]
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class ProfileCallsAttribute(Type type, params string[] methods) : Attribute
{
    public Type Type { get; } = type;

    public string[] Methods { get; } = methods;
}
