using System.Text.Json;

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
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static ParsedModule Parse(string path, string? json)
    {
        if (json is null)
            return new ParsedModule(path, null, "file could not be read");

        try
        {
            ModuleDto? dto = JsonSerializer.Deserialize<ModuleDto>(json, Options);

            if (dto is null || string.IsNullOrWhiteSpace(dto.Module))
                return new ParsedModule(path, null, "missing 'module' property");

            SlangModuleModel model = new(
                dto.Module,
                string.IsNullOrWhiteSpace(dto.Namespace) ? "Pathetic.Generated.Slang" : dto.Namespace,
                EquatableArray<TypeModel>.From((dto.Types ?? []).Select(static type => new TypeModel(
                    type.Name,
                    type.Size,
                    type.HasGpuLayout,
                    EquatableArray<FieldModel>.From((type.Fields ?? []).Select(static field => new FieldModel(
                        field.Name,
                        field.Offset,
                        field.CsType,
                        field.EnumType)))))),
                EquatableArray<BindingModel>.From((dto.Bindings ?? []).Select(static binding => new BindingModel(
                    binding.Name,
                    binding.Kind))));

            return new ParsedModule(path, model, null);
        }
        catch (JsonException exception)
        {
            return new ParsedModule(path, null, exception.Message);
        }
    }

    private sealed class ModuleDto
    {
        public string Module { get; set; } = "";
        public string Namespace { get; set; } = "";
        public List<TypeDto>? Types { get; set; }
        public List<BindingDto>? Bindings { get; set; }
    }

    private sealed class TypeDto
    {
        public string Name { get; set; } = "";
        public int? Size { get; set; }
        public bool HasGpuLayout { get; set; }
        public List<FieldDto>? Fields { get; set; }
    }

    private sealed class FieldDto
    {
        public string Name { get; set; } = "";
        public int Offset { get; set; }
        public string CsType { get; set; } = "";
        public string? EnumType { get; set; }
    }

    private sealed class BindingDto
    {
        public string Name { get; set; } = "";
        public string Kind { get; set; } = "";
    }
}
