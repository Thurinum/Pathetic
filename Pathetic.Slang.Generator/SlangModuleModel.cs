namespace Pathetic.Slang.Generator;

internal sealed record SlangModuleModel(
    string Module,
    string Namespace,
    EquatableArray<TypeModel> Types,
    EquatableArray<BindingModel> Bindings);

internal sealed record TypeModel(
    string Name,
    int? Size,
    bool HasGpuLayout,
    EquatableArray<FieldModel> Fields);

internal sealed record FieldModel(
    string Name,
    int Offset,
    string CsType,
    string? EnumType);

internal sealed record BindingModel(
    string Name,
    string Kind);

internal sealed record ParsedModule(string Path, SlangModuleModel? Model, string? Error);

internal static class ModuleParser
{
    public static ParsedModule Parse(string path, string? json)
    {
        if (json is null)
            return new ParsedModule(path, null, "file could not be read");

        try
        {
            if (JsonReader.Parse(json) is not JsonObject root)
                return new ParsedModule(path, null, "root value is not an object");

            string module = OptionalString(root, "module") ?? "";

            if (module.Length == 0)
                return new ParsedModule(path, null, "missing 'module' property");

            SlangModuleModel model = new(
                module,
                OptionalString(root, "namespace") ?? "Pathetic.Generated.Slang",
                EquatableArray<TypeModel>.From(ReadTypes(root)),
                EquatableArray<BindingModel>.From(ReadBindings(root)));

            return new ParsedModule(path, model, null);
        }
        catch (FormatException exception)
        {
            return new ParsedModule(path, null, exception.Message);
        }
    }

    private static IEnumerable<TypeModel> ReadTypes(JsonObject root)
    {
        if (root["types"] is not JsonArray types)
            yield break;

        foreach (JsonValue item in types.Items)
        {
            if (item is not JsonObject type)
                continue;

            yield return new TypeModel(
                OptionalString(type, "name") ?? "",
                OptionalInt(type, "size"),
                OptionalBool(type, "hasGpuLayout"),
                EquatableArray<FieldModel>.From(ReadFields(type)));
        }
    }

    private static IEnumerable<FieldModel> ReadFields(JsonObject type)
    {
        if (type["fields"] is not JsonArray fields)
            yield break;

        foreach (JsonValue item in fields.Items)
        {
            if (item is not JsonObject field)
                continue;

            yield return new FieldModel(
                OptionalString(field, "name") ?? "",
                OptionalInt(field, "offset") ?? 0,
                OptionalString(field, "csType") ?? "",
                OptionalString(field, "enumType"));
        }
    }

    private static IEnumerable<BindingModel> ReadBindings(JsonObject root)
    {
        if (root["bindings"] is not JsonArray bindings)
            yield break;

        foreach (JsonValue item in bindings.Items)
        {
            if (item is not JsonObject binding)
                continue;

            yield return new BindingModel(
                OptionalString(binding, "name") ?? "",
                OptionalString(binding, "kind") ?? "");
        }
    }

    private static string? OptionalString(JsonObject value, string name) =>
        value[name] is JsonString text ? text.Value : null;

    private static int? OptionalInt(JsonObject value, string name) =>
        value[name] is JsonNumber number ? (int)number.Value : null;

    private static bool OptionalBool(JsonObject value, string name) =>
        value[name] is JsonBool boolean && boolean.Value;
}
