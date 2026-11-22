using System.Text.Json;
using System.Text.Json.Serialization;
using Prowl.Slang;

namespace Pathetic.Slang.Tool;

internal sealed class Reflector(string targetNamespace)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HashSet<string> _cpuTypeNames = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ModuleJson> _modules = new(StringComparer.Ordinal);
    private readonly Dictionary<string, byte[]> _code = new(StringComparer.Ordinal);

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

        WriteOutput(outDir);
    }

    private void ReflectModule(Session session, ShaderFile file)
    {
        string source = File.ReadAllText(file.Path);
        Module module = LoadModule(session, file, source);

        ModuleJson json = new()
        {
            Module = file.ModuleName,
            Namespace = targetNamespace,
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

        Module module;

        try
        {
            module = session.LoadModuleFromSourceString(file.ModuleName, file.Path, source, out DiagnosticInfo diagnostics);
            ThrowOnErrors(diagnostics, file.Path);
        }
        catch (CompilationException exception)
        {
            throw new ToolException($"{file.Path}: {exception.Message}");
        }

        return module;
    }

    private void TryExportType(Session session, ShaderFile file, ModuleJson json, DeclReflection declaration)
    {
        TypeReflection type = declaration.Type;
        string? cpuTypeName = GetCPUTypeName(type);

        if (cpuTypeName is null)
            return;

        if (!_cpuTypeNames.Add(cpuTypeName))
            throw new ToolException($"Two [CPUType] declarations emit '{cpuTypeName}'; give one an explicit name with [CPUType(\"...\")].");

        TypeLayoutReflection layout = session.GetTypeLayout(type, 0, LayoutRules.Default, out DiagnosticInfo diagnostics);
        ThrowOnErrors(diagnostics, file.Path);

        List<FieldJson> fields = [];
        int end = 0;
        int alignment = 1;

        foreach (VariableLayoutReflection fieldLayout in layout.Fields)
        {
            TypeReflection fieldType = fieldLayout.Variable.Type;
            TypeLayoutReflection fieldTypeLayout = fieldLayout.TypeLayout;
            int offset = (int)fieldLayout.GetOffset(ParameterCategory.Uniform);

            end = Math.Max(end, offset + (int)fieldTypeLayout.GetSize(ParameterCategory.Uniform));
            alignment = Math.Max(alignment, (int)fieldTypeLayout.GetAlignment(ParameterCategory.Uniform));

            fields.Add(new FieldJson
            {
                Name = fieldLayout.Name,
                Offset = offset,
                CsType = MapFieldType(file.ModuleName, declaration.Name, fieldLayout),
                EnumType = Enum.IsDefined(fieldType.Kind) ? null : fieldType.Name,
            });
        }

        json.Types.Add(new TypeJson
        {
            Name = cpuTypeName,
            Size = (end + alignment - 1) / alignment * alignment,
            Fields = fields,
        });
    }

    private static string? GetCPUTypeName(TypeReflection type)
    {
        foreach (Prowl.Slang.Attribute attribute in type.UserAttributes)
        {
            if (!string.Equals(attribute.Name, "CPUType", StringComparison.OrdinalIgnoreCase))
                continue;

            string? uniqueName = attribute.ArgumentCount > 0 ? attribute.GetArgumentValueString(0) : null;
            return string.IsNullOrWhiteSpace(uniqueName) ? type.Name : uniqueName;
        }

        return null;
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

        byte[] code = linked.GetEntryPointCode(IntPtr.Zero, IntPtr.Zero, out DiagnosticInfo codeDiagnostics).ToArray();
        ThrowOnErrors(codeDiagnostics, file.Path);

        _code[file.OutputStem] = code;

    }

    private static string MapFieldType(string module, string typeName, VariableLayoutReflection fieldLayout)
    {
        TypeReflection fieldType = fieldLayout.Variable.Type;
        TypeLayoutReflection fieldTypeLayout = fieldLayout.TypeLayout;
        string fieldName = fieldLayout.Name;

        if (fieldType.Kind == TypeKind.Array || fieldTypeLayout.IsArray)
            throw new ToolException($"Exported type '{module}.{typeName}' field '{fieldName}' is an array, which is not supported yet.");

        if (fieldType.Kind == TypeKind.Matrix)
            throw new ToolException($"Exported type '{module}.{typeName}' field '{fieldName}' is a matrix, which is not supported yet.");

        if (!Enum.IsDefined(fieldType.Kind))
            return "int";

        if (fieldType.Kind == TypeKind.Vector)
        {
            if (fieldTypeLayout.ScalarType != ScalarType.Float32)
                throw new ToolException($"Exported type '{module}.{typeName}' field '{fieldName}' is a non-float vector, which is not supported yet.");

            return fieldTypeLayout.ColumnCount switch
            {
                2 => "global::System.Numerics.Vector2",
                3 => "global::System.Numerics.Vector3",
                4 => "global::System.Numerics.Vector4",
                _ => throw new ToolException($"Exported type '{module}.{typeName}' field '{fieldName}' has an unsupported vector width ({fieldTypeLayout.ColumnCount})."),
            };
        }

        if (fieldType.Kind == TypeKind.Scalar)
            return MapScalar(module, typeName, fieldName, fieldTypeLayout.ScalarType);

        if (fieldType.Kind == TypeKind.Struct)
            throw new ToolException($"Exported type '{module}.{typeName}' field '{fieldName}' is a nested struct, which is not supported yet.");

        throw new ToolException($"Exported type '{module}.{typeName}' field '{fieldName}' has unsupported Slang type '{fieldType.Name}' (kind {fieldType.Kind}).");
    }

    private static string MapScalar(string module, string typeName, string fieldName, ScalarType scalar) => scalar switch
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
        _ => throw new ToolException($"Exported type '{module}.{typeName}' field '{fieldName}' uses unsupported scalar '{scalar}'."),
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

        string codeDir = Path.Combine(outDir, "gen");
        Directory.CreateDirectory(codeDir);

        IndexJson index = new() { Namespace = targetNamespace };

        foreach (ModuleJson module in _modules.Values.OrderBy(static module => module.Module, StringComparer.Ordinal))
        {
            string stem = Path.GetFileNameWithoutExtension(module.SourceFile);
            File.WriteAllText(Path.Combine(outDir, stem + ".json"), JsonSerializer.Serialize(module, JsonOptions));
            File.WriteAllText(Path.Combine(codeDir, stem + ".g.cs"), SourceEmitter.EmitModule(targetNamespace, module));

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
