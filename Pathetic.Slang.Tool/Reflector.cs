using System.Text.Json;
using System.Text.Json.Serialization;
using Prowl.Slang;

namespace Pathetic.Slang.Tool;

internal sealed class ExportedField
{
    public required string Name { get; init; }
    public required TypeKind Kind { get; init; }
    public required ScalarType Scalar { get; init; }
    public required string TypeName { get; init; }
    public required bool IsArray { get; init; }
    public required bool IsEnum { get; init; }
    public required int Elements { get; init; }
}

internal sealed class ExportedType
{
    public required string Module { get; init; }
    public required string Name { get; init; }
    public required List<ExportedField> Fields { get; init; }
}

internal sealed class ObservedLayout
{
    public required string Module { get; init; }
    public required StructLayout Layout { get; init; }
}

internal sealed class Reflector
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _namespace;
    private readonly List<ExportedType> _exportedTypes = [];
    private readonly List<ObservedLayout> _observedLayouts = [];
    private readonly Dictionary<string, ModuleJson> _modules = new(StringComparer.Ordinal);
    private readonly Dictionary<string, byte[]> _code = new(StringComparer.Ordinal);

    public Reflector(string targetNamespace)
    {
        _namespace = targetNamespace;
    }

    public void Run(ShaderSet shaders, string outDir)
    {
        SessionDescription description = new()
        {
            Targets = [new TargetDescription { Format = CompileTarget.Spirv, Profile = GlobalSession.FindProfile("spirv_1_5") }],
            SearchPaths = [.. shaders.Roots, "./"],
        };

        Session session = GlobalSession.CreateSession(description);

        foreach (ShaderFile file in shaders.Files)
            ReflectModule(session, file);

        foreach (ModuleJson module in _modules.Values)
            ResolveTypes(module);

        WriteOutput(outDir);
    }

    private void ReflectModule(Session session, ShaderFile file)
    {
        string source = File.ReadAllText(file.Path);
        Module module = LoadModule(session, file, source);

        ModuleJson json = new()
        {
            Module = file.ModuleName,
            Namespace = _namespace,
            SourceFile = file.Path,
        };

        DeclReflection root = module.GetModuleReflection();

        foreach (DeclReflection declaration in root.Children)
        {
            switch (declaration.Kind)
            {
                case DeclKind.Struct:
                    TryExportType(session, file, json, declaration);
                    break;
                case DeclKind.Variable:
                {
                    VariableReflection variable = declaration.AsVariable();

                    if (!IsResourceType(variable.Type.Kind))
                        break;

                    json.Bindings.Add(new BindingJson
                    {
                        Name = variable.Name,
                        Kind = ClassifyBinding(variable.Type),
                    });
                    break;
                }
            }
        }

        if (module.GetDefinedEntryPointCount() > 0)
            ReflectPipeline(session, file, module, json);

        _modules[file.ModuleName] = json;
    }

    private static Module LoadModule(Session session, ShaderFile file, string source)
    {
        for (int i = 0; i < session.GetLoadedModuleCount(); i++)
        {
            Module loaded = session.GetLoadedModule(i);
            string? path = loaded.GetFilePath();

            if (path is not null && string.Equals(Path.GetFullPath(path), file.Path, StringComparison.OrdinalIgnoreCase))
                return loaded;
        }

        Module module = session.LoadModuleFromSourceString(file.ModuleName, file.Path, source, out DiagnosticInfo diagnostics);
        ThrowOnErrors(diagnostics, file.Path);
        return module;
    }

    private void TryExportType(Session session, ShaderFile file, ModuleJson json, DeclReflection declaration)
    {
        TypeReflection type = declaration.Type;

        if (!HasExportAttribute(type))
            return;

        TypeLayoutReflection layout = session.GetTypeLayout(type, 0, LayoutRules.Default, out DiagnosticInfo diagnostics);
        ThrowOnErrors(diagnostics, file.Path);

        List<ExportedField> fields = [];

        foreach (VariableLayoutReflection fieldLayout in layout.Fields)
        {
            TypeReflection fieldType = fieldLayout.Variable.Type;
            TypeLayoutReflection fieldTypeLayout = fieldLayout.TypeLayout;

            fields.Add(new ExportedField
            {
                Name = fieldLayout.Name,
                Kind = fieldType.Kind,
                Scalar = fieldTypeLayout.ScalarType,
                TypeName = fieldType.Name,
                IsArray = fieldType.Kind == TypeKind.Array || fieldTypeLayout.IsArray,
                IsEnum = !Enum.IsDefined(fieldType.Kind),
                Elements = (int)fieldTypeLayout.ColumnCount,
            });
        }

        _exportedTypes.Add(new ExportedType
        {
            Module = file.ModuleName,
            Name = declaration.Name,
            Fields = fields,
        });

        json.Types.Add(new TypeJson
        {
            Name = declaration.Name,
            HasGpuLayout = false,
            Fields = [],
        });
    }

    private static bool HasExportAttribute(TypeReflection type)
    {
        foreach (Prowl.Slang.Attribute attribute in type.UserAttributes)
        {
            if (string.Equals(attribute.Name, "Export", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void ReflectPipeline(Session session, ShaderFile file, Module module, ModuleJson json)
    {
        EntryPoint entryPoint = module.GetDefinedEntryPoint(0);

        ComponentType composite = session.CreateCompositeComponentType([module, entryPoint], out DiagnosticInfo componentDiagnostics);
        ThrowOnErrors(componentDiagnostics, file.Path);

        ComponentType linked = composite.Link(out DiagnosticInfo linkDiagnostics);
        ThrowOnErrors(linkDiagnostics, file.Path);

        ShaderReflection program = linked.GetLayout();
        EntryPointReflection entryPointReflection = program.EntryPoints.First();
        (uint x, uint y, uint z) = entryPointReflection.GetComputeThreadGroupSize();

        PipelineJson pipeline = new()
        {
            EntryPoint = entryPointReflection.Name,
            ThreadGroupSize = [x, y, z],
        };

        for (uint i = 0; i < program.ParameterCount; i++)
        {
            VariableLayoutReflection parameter = program.GetParameterByIndex(i);

            if (parameter.Category != ParameterCategory.DescriptorTableSlot)
                continue;

            pipeline.Parameters.Add(new ParameterJson
            {
                Name = parameter.Name,
                Kind = ClassifyBinding(parameter.TypeLayout.GetBindingRangeType(0)),
                Space = parameter.BindingSpace,
                Binding = parameter.BindingIndex,
            });
        }

        json.Pipeline = pipeline;

        Memory<byte> code = linked.GetEntryPointCode(IntPtr.Zero, IntPtr.Zero, out DiagnosticInfo codeDiagnostics);
        ThrowOnErrors(codeDiagnostics, file.Path);

        _code[file.OutputStem] = code.ToArray();

        SpirvModule spirv = SpirvModule.Parse(System.Runtime.InteropServices.MemoryMarshal.Cast<byte, uint>(code.Span));

        foreach (StructLayout layout in spirv.BufferElementLayouts())
            _observedLayouts.Add(new ObservedLayout { Module = file.ModuleName, Layout = layout });
    }

    private void ResolveTypes(ModuleJson module)
    {
        foreach (TypeJson type in module.Types)
        {
            ExportedType exported = _exportedTypes.Single(candidate => candidate.Module == module.Module && candidate.Name == type.Name);

            StructLayout? layout = ResolveLayout(exported);

            type.Fields.AddRange(MapFields(exported));

            if (layout is null)
            {
                type.HasGpuLayout = false;
                type.Size = null;
                continue;
            }

            type.HasGpuLayout = true;
            type.Size = layout.Size;

            for (int i = 0; i < type.Fields.Count; i++)
            {
                FieldJson field = type.Fields[i];
                type.Fields[i] = new FieldJson
                {
                    Name = field.Name,
                    Offset = layout.Offsets[i],
                    CsType = field.CsType,
                    EnumType = field.EnumType,
                };
            }
        }
    }

    private StructLayout? ResolveLayout(ExportedType type)
    {
        List<StructLayout> candidates = [];

        foreach (ObservedLayout observed in _observedLayouts)
        {
            if (!IsLayoutCandidate(observed.Layout.Name, type.Name))
                continue;

            if (observed.Layout.Offsets.Length != type.Fields.Count)
                continue;

            candidates.Add(observed.Layout);
        }

        if (candidates.Count == 0)
            return null;

        StructLayout first = candidates[0];

        foreach (StructLayout candidate in candidates)
        {
            if (!candidate.Offsets.AsSpan().SequenceEqual(first.Offsets))
            {
                throw new ToolException(
                    $"Exported type '{type.Module}.{type.Name}' has conflicting layouts: " +
                    $"[{string.Join(", ", first.Offsets)}] vs [{string.Join(", ", candidate.Offsets)}].");
            }
        }

        return first;
    }

    private static bool IsLayoutCandidate(string spirvName, string typeName)
    {
        if (string.Equals(spirvName, typeName, StringComparison.Ordinal))
            return true;

        return spirvName.Length > typeName.Length
            && spirvName.StartsWith(typeName, StringComparison.Ordinal)
            && spirvName[typeName.Length] == '_';
    }

    private List<FieldJson> MapFields(ExportedType type)
    {
        List<FieldJson> fields = [];

        foreach (ExportedField field in type.Fields)
        {
            fields.Add(new FieldJson
            {
                Name = field.Name,
                Offset = 0,
                CsType = MapFieldType(type, field),
                EnumType = field.IsEnum ? field.TypeName : null,
            });
        }

        return fields;
    }

    private string MapFieldType(ExportedType owner, ExportedField field)
    {
        if (field.IsArray)
            throw new ToolException($"Exported type '{owner.Module}.{owner.Name}' field '{field.Name}' is an array, which is not supported yet.");

        if (field.Kind == TypeKind.Matrix)
            throw new ToolException($"Exported type '{owner.Module}.{owner.Name}' field '{field.Name}' is a matrix, which is not supported yet.");

        if (field.IsEnum)
            return "int";

        if (field.Kind == TypeKind.Vector)
        {
            if (field.Scalar != ScalarType.Float32)
                throw new ToolException($"Exported type '{owner.Module}.{owner.Name}' field '{field.Name}' is a non-float vector, which is not supported yet.");

            return field.Elements switch
            {
                2 => "global::System.Numerics.Vector2",
                3 => "global::System.Numerics.Vector3",
                4 => "global::System.Numerics.Vector4",
                _ => throw new ToolException($"Exported type '{owner.Module}.{owner.Name}' field '{field.Name}' has an unsupported vector width ({field.Elements})."),
            };
        }

        if (field.Kind == TypeKind.Scalar)
            return MapScalar(owner, field);

        if (field.Kind == TypeKind.Struct)
            throw new ToolException($"Exported type '{owner.Module}.{owner.Name}' field '{field.Name}' is a nested struct, which is not supported yet.");

        throw new ToolException($"Exported type '{owner.Module}.{owner.Name}' field '{field.Name}' has unsupported Slang type '{field.TypeName}' (kind {field.Kind}).");
    }

    private static string MapScalar(ExportedType owner, ExportedField field) => field.Scalar switch
    {
        ScalarType.Float32 => "float",
        ScalarType.Float64 => "double",
        ScalarType.Int32 => "int",
        ScalarType.UInt32 => "uint",
        ScalarType.Int64 => "long",
        ScalarType.UInt64 => "ulong",
        ScalarType.Int16 => "short",
        ScalarType.UInt16 => "ushort",
        ScalarType.Int8 => "sbyte",
        ScalarType.UInt8 => "byte",
        ScalarType.Bool => "bool",
        _ => throw new ToolException($"Exported type '{owner.Module}.{owner.Name}' field '{field.Name}' uses unsupported scalar '{field.Scalar}'."),
    };

    private static bool IsResourceType(TypeKind kind) => kind is
        TypeKind.ConstantBuffer or
        TypeKind.Resource or
        TypeKind.SamplerState or
        TypeKind.TextureBuffer or
        TypeKind.ShaderStorageBuffer or
        TypeKind.ParameterBlock;

    private static string ClassifyBinding(TypeReflection type)
    {
        if (type.Kind == TypeKind.ConstantBuffer)
            return "UniformBuffer";

        if (type.Kind == TypeKind.SamplerState)
            return "Sampler";

        if (type.Kind == TypeKind.ParameterBlock)
            return "ParameterBlock";

        if (type.Kind != TypeKind.Resource)
            throw new ToolException($"Unsupported global resource type '{type.FullName}' (kind {type.Kind}).");

        bool writable = type.ResourceAccess is ResourceAccess.ReadWrite or ResourceAccess.RasterOrdered or ResourceAccess.Write or ResourceAccess.Append or ResourceAccess.Consume;

        return type.ResourceShape switch
        {
            ResourceShape.StructuredBuffer or ResourceShape.ByteAddressBuffer or ResourceShape.TextureBuffer =>
                writable ? "BufferReadWrite" : "BufferRead",
            ResourceShape.AccelerationStructure => throw new ToolException("Ray tracing acceleration structures are not supported in the reflection pipeline."),
            _ => writable ? "MutableTexture" : "Texture",
        };
    }

    private static string ClassifyBinding(BindingType type) => type switch
    {
        BindingType.MutableTexture => "MutableTexture",
        BindingType.Texture => "Texture",
        BindingType.Sampler => "Sampler",
        BindingType.ConstantBuffer => "UniformBuffer",
        BindingType.TypedBuffer or BindingType.RawBuffer => "BufferRead",
        BindingType.MutableTypedBuffer or BindingType.MutableRawBuffer => "BufferReadWrite",
        BindingType.ParameterBlock => "ParameterBlock",
        _ => throw new ToolException($"Unsupported shader parameter binding type '{type}'."),
    };

    private void WriteOutput(string outDir)
    {
        Directory.CreateDirectory(outDir);

        IndexJson index = new() { Namespace = _namespace };

        foreach (ModuleJson module in _modules.Values.OrderBy(static module => module.Module, StringComparer.Ordinal))
        {
            string path = Path.Combine(outDir, module.SourceFile is null ? module.Module + ".json" : Path.GetFileNameWithoutExtension(module.SourceFile) + ".json");
            File.WriteAllText(path, JsonSerializer.Serialize(module, JsonOptions));

            string stem = Path.GetFileNameWithoutExtension(path);

            if (_code.TryGetValue(stem, out byte[]? code))
                File.WriteAllBytes(Path.Combine(outDir, stem + ".spv"), code);

            index.Modules.Add(new IndexEntryJson { Module = module.Module, File = stem });
        }

        File.WriteAllText(Path.Combine(outDir, "index.json"), JsonSerializer.Serialize(index, JsonOptions));
    }

    private static void ThrowOnErrors(DiagnosticInfo diagnostics, string path)
    {
        List<string> errors = [];

        foreach (Diagnostic diagnostic in diagnostics.GetDiagnostics())
        {
            if (diagnostic.Severity >= Severity.Error)
                errors.Add(diagnostic.Message);
        }

        if (errors.Count > 0)
            throw new ToolException($"{path}: {string.Join(Environment.NewLine, errors)}");
    }
}
