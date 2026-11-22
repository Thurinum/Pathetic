namespace Pathetic.Slang.Tool;

internal sealed class ModuleJson
{
    public required string Module { get; init; }
    public required string Namespace { get; init; }
    public required string SourceFile { get; init; }
    public PipelineJson? Pipeline { get; set; }
    public List<BindingJson> Bindings { get; init; } = [];
    public List<TypeJson> Types { get; init; } = [];
}

internal sealed class PipelineJson
{
    public required string EntryPoint { get; init; }
    public required uint[] ThreadGroupSize { get; init; }
    public List<ParameterJson> Parameters { get; init; } = [];
}

internal sealed class BindingJson
{
    public required string Name { get; init; }
    public required string Kind { get; init; }
}

internal sealed class ParameterJson
{
    public required string Name { get; init; }
    public required string Kind { get; init; }
    public required uint Space { get; init; }
    public required uint Binding { get; init; }
}

internal sealed class TypeJson
{
    public required string Name { get; init; }
    public required int Size { get; init; }
    public List<FieldJson> Fields { get; init; } = [];
}

internal sealed class FieldJson
{
    public required string Name { get; init; }
    public int Offset { get; init; }
    public required string CsType { get; init; }
    public string? EnumType { get; init; }
}

internal sealed class IndexJson
{
    public required string Namespace { get; init; }
    public List<IndexEntryJson> Modules { get; init; } = [];
}

internal sealed class IndexEntryJson
{
    public required string Module { get; init; }
    public required string File { get; init; }
}
