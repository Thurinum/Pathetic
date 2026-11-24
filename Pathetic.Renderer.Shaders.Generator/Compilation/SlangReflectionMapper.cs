using Pathetic.Renderer.Shaders.Generator.Diagnostics;
using Pathetic.Renderer.Shaders.Generator.Models;
using Prowl.Slang;
using Attribute = Prowl.Slang.Attribute;

namespace Pathetic.Renderer.Shaders.Generator.Compilation;

internal static class SlangReflectionMapper
{
    private const string CpuTypeAttributeName = "CPUType";
    private const TypeKind SlangEnumKind = (TypeKind)20;

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

    public static ModuleModel ReadModule(Session session, ShaderSource source, Module module, PipelineModel? pipeline)
    {
        List<ResourceBinding> bindings = [];
        List<TypeModel> types = [];

        foreach (DeclReflection declaration in module.GetModuleReflection().Children)
        {
            if (declaration.Kind == DeclKind.Struct)
            {
                if (ReadCpuType(session, source, declaration) is { } type)
                    types.Add(type);
            }
            else if (declaration.Kind == DeclKind.Variable)
            {
                VariableReflection variable = declaration.AsVariable();

                if (GetBindingKind(variable.Type) is { } kind)
                    bindings.Add(new ResourceBinding(variable.Name, kind));
            }
        }

        return new ModuleModel(source.ModuleName, pipeline, bindings, types);
    }

    public static PipelineModel ReadPipeline(ShaderReflection program)
    {
        (uint x, uint y, uint z) = program.EntryPoints.First().GetComputeThreadGroupSize();
        List<PipelineParameter> parameters = [];

        for (uint i = 0; i < program.ParameterCount; i++)
        {
            VariableLayoutReflection parameter = program.GetParameterByIndex(i);

            if (parameter.Category != ParameterCategory.DescriptorTableSlot)
                continue;

            BindingKind kind = GetBindingKind(parameter.Type)
                ?? throw new ShaderCompilerException($"Shader parameter '{parameter.Name}' is not a supported resource.");

            parameters.Add(new PipelineParameter(parameter.Name, kind, parameter.BindingIndex, parameter.BindingSpace));
        }

        return new PipelineModel(new ThreadGroupSize(x, y, z), parameters);
    }

    private static TypeModel? ReadCpuType(Session session, ShaderSource source, DeclReflection declaration)
    {
        TypeReflection type = declaration.Type;

        if (ReadCpuTypeName(type) is not { } cpuTypeName)
            return null;

        TypeLayoutReflection layout = session.GetTypeLayout(type, 0, LayoutRules.Default, out DiagnosticInfo diagnostics);
        SlangDiagnostics.ThrowIfErrors(diagnostics);

        List<FieldModel> fields = [];
        int end = 0;
        int alignment = 1;

        foreach (VariableLayoutReflection fieldLayout in layout.Fields)
        {
            TypeLayoutReflection fieldTypeLayout = fieldLayout.TypeLayout;
            int offset = (int)fieldLayout.GetOffset();

            end = Math.Max(end, offset + (int)fieldTypeLayout.GetSize());
            alignment = Math.Max(alignment, fieldTypeLayout.GetAlignment(ParameterCategory.Uniform));

            fields.Add(new FieldModel(
                fieldLayout.Name,
                offset,
                MapFieldType($"{source.ModuleName}.{declaration.Name}.{fieldLayout.Name}", fieldLayout)));
        }

        return new TypeModel(cpuTypeName, (end + alignment - 1) / alignment * alignment, fields);
    }

    private static string? ReadCpuTypeName(TypeReflection type)
    {
        foreach (Attribute attribute in type.UserAttributes)
        {
            if (!string.Equals(attribute.Name, CpuTypeAttributeName, StringComparison.OrdinalIgnoreCase))
                continue;

            string? uniqueName = attribute.ArgumentCount > 0 ? attribute.GetArgumentValueString(0) : null;
            return string.IsNullOrWhiteSpace(uniqueName) ? type.Name : uniqueName;
        }

        return null;
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
            _ => throw new ShaderCompilerException($"Field '{field}' uses unsupported Slang type '{type.Name}' (kind {type.Kind})."),
        };
    }

    private static string MapScalar(string field, ScalarType scalar) =>
        ScalarTypes.TryGetValue(scalar, out string? type)
            ? type
            : throw new ShaderCompilerException($"Field '{field}' uses unsupported scalar '{scalar}'.");

    private static string MapVector(string field, TypeLayoutReflection layout)
    {
        if (layout.ScalarType != ScalarType.Float32 || !VectorTypes.TryGetValue(layout.ColumnCount, out string? type))
            throw new ShaderCompilerException($"Field '{field}' uses unsupported vector '{layout.ScalarType}{layout.ColumnCount}'.");

        return type;
    }

    private static BindingKind? GetBindingKind(TypeReflection type)
    {
        while (type.Kind == TypeKind.Array)
            type = type.ElementType;

        return type.Kind switch
        {
            TypeKind.ConstantBuffer => BindingKind.UniformBuffer,
            TypeKind.SamplerState => BindingKind.Sampler,
            TypeKind.Resource or TypeKind.ShaderStorageBuffer or TypeKind.TextureBuffer => GetResourceKind(type),
            TypeKind.ParameterBlock => throw new ShaderCompilerException($"Parameter blocks are not supported ('{type.FullName}')."),
            _ => null,
        };
    }

    private static BindingKind GetResourceKind(TypeReflection type)
    {
        bool writable = type.ResourceAccess is ResourceAccess.ReadWrite or ResourceAccess.RasterOrdered or ResourceAccess.Write or ResourceAccess.Append or ResourceAccess.Consume;

        return type.ResourceShape switch
        {
            ResourceShape.StructuredBuffer or ResourceShape.ByteAddressBuffer or ResourceShape.TextureBuffer => writable ? BindingKind.MutableBuffer : BindingKind.Buffer,
            ResourceShape.AccelerationStructure => throw new ShaderCompilerException($"Ray tracing acceleration structures are not supported ('{type.FullName}')."),
            ResourceShape.None or ResourceShape.Unknown => throw new ShaderCompilerException($"Unsupported resource shape '{type.ResourceShape}' ('{type.FullName}')."),
            _ => writable ? BindingKind.MutableTexture : BindingKind.Texture,
        };
    }
}
