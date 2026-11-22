using System.Diagnostics;

namespace Pathetic.Profiling.Aspects;

[Conditional(ProfilingName.Symbol)]
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class ProfileAttribute : Attribute
{
    public bool Frame { get; init; }
}
