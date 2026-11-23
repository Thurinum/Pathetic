using System.Diagnostics;

namespace Pathetic.Profiling.Aspects;

[Conditional(ProfilingName.Symbol)]
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class ProfileTagAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
