namespace Pathetic.Profiling.Aspects;

/// <summary>Shows the property as a row of the shader counters table. <c>Rows()</c> is generated from these.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class ShaderCounterAttribute(string name) : Attribute
{
    public string Name { get; } = name;

    public int Order { get; init; }

    public string? Format { get; init; }

    public string Suffix { get; init; } = string.Empty;
}
