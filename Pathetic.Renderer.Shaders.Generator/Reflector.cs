using System.Text;
using Prowl.Slang;

namespace Pathetic.Renderer.Shaders.Generator;

internal sealed class Reflector
{
    private const string CpuTypeAttribute = "CPUType";
    private const TypeKind SlangEnumKind = (TypeKind)20;

    private static readonly UTF8Encoding Utf8Bom = new(encoderShouldEmitUTF8Identifier: true);
    private static readonly Dictionary<ScalarType, string> ScalarTypes = new()
    {
        [ScalarType.Float32] = "float",
        [ScalarType.Float64] = "double",
        [ScalarType.Int32] = "int",
        [ScalarType.UInt32] = "uint",
        [ScalarType.Int64] = "long",
        [ScalarType.UInt64] = "ulong",
        [ScalarType.Int16] = "short",
        [ScalarType.UInt16] = "ushort",
        [ScalarType.Int8] = "sbyte",
        [ScalarType.UInt8] = "byte",
        [ScalarType.Bool] = "bool",
    };
    private static readonly Dictionary<uint, string> VectorTypes = new()
    {
        [2] = "global::System.Numerics.Vector2",
        [3] = "global::System.Numerics.Vector3",
        [4] = "global::System.Numerics.Vector4",
    };

    private readonly HashSet<string> _cpuTypeNames = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ModuleModel> _modules = new(StringComparer.Ordinal);
    private readonly Dictionary<string, byte[]> _code = new(StringComparer.Ordinal);

    public void Run(ShaderSet shaders, string outDir)
    {
        TargetDescription targetDesc = new()
        {
            Format = CompileTarget.Spirv,
            Profile = GlobalSession.FindProfile("spirv_1_5")
        };
        SessionDescription sessionDesc = new()
        {
            Targets = [targetDesc],
            SearchPaths = [.. shaders.Roots],
        };

        Session session = GlobalSession.CreateSession(sessionDesc);

        foreach (ShaderFile file in shaders.Files)
            ReflectModule(session, file);

        WriteOutput(outDir);
    }

    private void ReflectModule(Session session, ShaderFile file)
    {
        string source = File.ReadAllText(file.Path);
        Module module = LoadModule(session, file, source);

        ModuleModel model = new()
        {
            Name = file.Name,
        };

        DeclReflection root = module.GetModuleReflection();

        foreach (DeclReflection declaration in root.Children)
        {
            switch (declaration.Kind)
            {
                case DeclKind.Struct:
                    TryExportType(session, file, model, declaration);
                    break;
                case DeclKind.Variable:
                {
                    VariableReflection variable = declaration.AsVariable();

                    if (ClassifyBinding(variable.Type) is { } kind)
                        model.Bindings.Add(new BindingModel { Name = variable.Name, Kind = kind });

                    break;
                }
            }
        }

        if (module.GetDefinedEntryPointCount() > 0)
            ReflectPipeline(session, file, module, model);

        _modules[file.Name] = model;
    }

    private static Module LoadModule(Session session, ShaderFile file, string source)
    {
        for (int i = 0; i < session.GetLoadedModuleCount(); i++)
        {
            Module loaded = session.GetLoadedModule(i);
            string path = loaded.GetFilePath();

            if (string.Equals(Path.GetFullPath(path), file.Path, StringComparison.OrdinalIgnoreCase))
                return loaded;
        }

        Module module;

        try
        {
            module = session.LoadModuleFromSourceString(file.Name, file.Path, source, out DiagnosticInfo diagnostics);
            ThrowOnErrors(diagnostics);
        }
        catch (CompilationException exception)
        {
            throw new ShaderCompilationException(DiagnosticText.Format(exception.Message));
        }

        return module;
    }

    private void TryExportType(Session session, ShaderFile file, ModuleModel model, DeclReflection declaration)
    {
        TypeReflection type = declaration.Type;
        string? cpuTypeName = GetCPUTypeName(type);

        if (cpuTypeName is null)
            return;

        if (!_cpuTypeNames.Add(cpuTypeName))
            throw new SlangInteropException($"Two [CPUType] declarations emit '{cpuTypeName}'; give one an explicit name with [CPUType(\"...\")].");

        TypeLayoutReflection layout = session.GetTypeLayout(type, 0, LayoutRules.Default, out DiagnosticInfo diagnostics);
        ThrowOnErrors(diagnostics);

        List<FieldModel> fields = [];
        int end = 0;
        int alignment = 1;

        foreach (VariableLayoutReflection fieldLayout in layout.Fields)
        {
            TypeLayoutReflection fieldTypeLayout = fieldLayout.TypeLayout;
            int offset = (int)fieldLayout.GetOffset(ParameterCategory.Uniform);

            end = Math.Max(end, offset + (int)fieldTypeLayout.GetSize(ParameterCategory.Uniform));
            alignment = Math.Max(alignment, (int)fieldTypeLayout.GetAlignment(ParameterCategory.Uniform));

            fields.Add(new FieldModel
            {
                Name = fieldLayout.Name,
                Offset = offset,
                CsType = MapFieldType($"{file.Name}.{declaration.Name}.{fieldLayout.Name}", fieldLayout),
            });
        }

        model.Types.Add(new TypeModel
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
            if (!string.Equals(attribute.Name, CpuTypeAttribute, StringComparison.OrdinalIgnoreCase))
                continue;

            string? uniqueName = attribute.ArgumentCount > 0 ? attribute.GetArgumentValueString(0) : null;
            return string.IsNullOrWhiteSpace(uniqueName) ? type.Name : uniqueName;
        }

        return null;
    }

    private void ReflectPipeline(Session session, ShaderFile file, Module module, ModuleModel model)
    {
        EntryPoint entryPoint = module.GetDefinedEntryPoint(0);

        ComponentType composite = session.CreateCompositeComponentType([module, entryPoint], out DiagnosticInfo componentDiagnostics);
        ThrowOnErrors(componentDiagnostics);

        ComponentType linked = composite.Link(out DiagnosticInfo linkDiagnostics);
        ThrowOnErrors(linkDiagnostics);

        ShaderReflection program = linked.GetLayout();
        EntryPointReflection entryPointReflection = program.EntryPoints.First();
        (uint x, uint y, uint z) = entryPointReflection.GetComputeThreadGroupSize();

        PipelineModel pipeline = new()
        {
            ThreadGroupSize = [x, y, z],
        };

        for (uint i = 0; i < program.ParameterCount; i++)
        {
            VariableLayoutReflection parameter = program.GetParameterByIndex(i);

            if (parameter.Category != ParameterCategory.DescriptorTableSlot)
                continue;

            BindingKind kind = ClassifyBinding(parameter.Type)
                ?? throw new SlangInteropException($"Shader parameter '{parameter.Name}' is not a supported resource.");

            pipeline.Parameters.Add(new ParameterModel
            {
                Name = parameter.Name,
                Kind = kind,
                Space = parameter.BindingSpace,
                Binding = parameter.BindingIndex,
            });
        }

        model.Pipeline = pipeline;

        byte[] code = linked.GetEntryPointCode(IntPtr.Zero, IntPtr.Zero, out DiagnosticInfo codeDiagnostics).ToArray();
        ThrowOnErrors(codeDiagnostics);

        _code[file.Name] = code;
    }

    private static string MapFieldType(string field, VariableLayoutReflection fieldLayout)
    {
        TypeReflection type = fieldLayout.Variable.Type;
        TypeLayoutReflection layout = fieldLayout.TypeLayout;

        return type.Kind switch
        {
            TypeKind.Scalar => MapScalar(field, layout.ScalarType),
            TypeKind.Vector => MapVector(field, layout),
            SlangEnumKind => MapScalar(field, layout.ScalarType),
            _ => throw new SlangInteropException($"Field '{field}' uses unsupported Slang type '{type.Name}' (kind {type.Kind})."),
        };
    }

    private static string MapScalar(string field, ScalarType scalar) =>
        ScalarTypes.TryGetValue(scalar, out string? type)
            ? type
            : throw new SlangInteropException($"Field '{field}' uses unsupported scalar '{scalar}'.");

    private static string MapVector(string field, TypeLayoutReflection layout)
    {
        if (layout.ScalarType != ScalarType.Float32 || !VectorTypes.TryGetValue(layout.ColumnCount, out string? type))
            throw new SlangInteropException($"Field '{field}' uses unsupported vector '{layout.ScalarType}{layout.ColumnCount}'.");

        return type;
    }

    private static BindingKind? ClassifyBinding(TypeReflection type)
    {
        while (type.Kind == TypeKind.Array)
            type = type.ElementType;

        return type.Kind switch
        {
            TypeKind.ConstantBuffer => BindingKind.UniformBuffer,
            TypeKind.SamplerState => BindingKind.Sampler,
            TypeKind.Resource or TypeKind.ShaderStorageBuffer or TypeKind.TextureBuffer => ClassifyResource(type),
            TypeKind.ParameterBlock => throw new SlangInteropException($"Parameter blocks are not supported ('{type.FullName}')."),
            _ => null,
        };
    }

    private static BindingKind ClassifyResource(TypeReflection type)
    {
        bool writable = type.ResourceAccess is ResourceAccess.ReadWrite or ResourceAccess.RasterOrdered or ResourceAccess.Write or ResourceAccess.Append or ResourceAccess.Consume;

        return type.ResourceShape switch
        {
            ResourceShape.StructuredBuffer or ResourceShape.ByteAddressBuffer or ResourceShape.TextureBuffer => writable ? BindingKind.MutableBuffer : BindingKind.Buffer,
            ResourceShape.AccelerationStructure => throw new SlangInteropException($"Ray tracing acceleration structures are not supported ('{type.FullName}')."),
            ResourceShape.None or ResourceShape.Unknown => throw new SlangInteropException($"Unsupported resource shape '{type.ResourceShape}' ('{type.FullName}')."),
            _ => writable ? BindingKind.MutableTexture : BindingKind.Texture,
        };
    }

    private void WriteOutput(string outDir)
    {
        Directory.CreateDirectory(outDir);

        string codeDir = Path.Combine(outDir, "gen");
        Directory.CreateDirectory(codeDir);

        foreach (ModuleModel module in _modules.Values.OrderBy(static module => module.Name, StringComparer.Ordinal))
        {
            File.WriteAllText(Path.Combine(codeDir, module.Name + ".g.cs"), SourceEmitter.EmitModule(module), Utf8Bom);

            if (_code.TryGetValue(module.Name, out byte[]? code))
                File.WriteAllBytes(Path.Combine(outDir, module.Name + ".spv"), code);
        }

        File.WriteAllText(Path.Combine(codeDir, "ShaderModules.g.cs"), SourceEmitter.EmitShaderModules(_modules.Values), Utf8Bom);
    }

    private static void ThrowOnErrors(DiagnosticInfo diagnostics)
    {
        List<string> errors = [];

        foreach (Diagnostic diagnostic in diagnostics.GetDiagnostics())
        {
            if (diagnostic.Severity >= Severity.Error)
                errors.Add(DiagnosticText.Format(diagnostic.Message));
        }

        if (errors.Count == 0 && DiagnosticText.HasErrors(diagnostics.Message))
            errors.Add(DiagnosticText.Format(diagnostics.Message));

        if (errors.Count > 0)
            throw new ShaderCompilationException(string.Join(Environment.NewLine, errors));
    }
}
