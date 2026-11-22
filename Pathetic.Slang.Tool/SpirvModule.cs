using System.Runtime.InteropServices;
using System.Text;

namespace Pathetic.Slang.Tool;

internal enum SpirvTypeClass
{
    Unknown,
    Scalar,
    Vector,
    Matrix,
    Array,
    RuntimeArray,
    Struct,
    Pointer,
}

internal sealed class SpirvType
{
    public SpirvTypeClass Class { get; init; }
    public int Width { get; init; }
    public uint ComponentId { get; init; }
    public uint Count { get; init; }
    public uint ElementId { get; init; }
}

internal sealed class StructLayout
{
    public required string Name { get; init; }
    public required int[] Offsets { get; init; }
    public required int Size { get; init; }
}

internal sealed class SpirvModule
{
    private readonly Dictionary<uint, string> _names = [];
    private readonly Dictionary<uint, SpirvType> _types = [];
    private readonly Dictionary<uint, uint[]> _structMembers = [];
    private readonly Dictionary<uint, Dictionary<int, int>> _memberOffsets = [];
    private readonly Dictionary<uint, int> _arrayStrides = [];
    private readonly Dictionary<uint, int> _matrixStrides = [];
    private readonly Dictionary<uint, ulong> _constants = [];
    private readonly List<uint> _variablePointerTypes = [];
    private readonly Dictionary<uint, int> _structSizes = [];

    public static SpirvModule Parse(ReadOnlySpan<uint> words)
    {
        SpirvModule module = new();
        module.Read(words);
        return module;
    }

    public IReadOnlyList<StructLayout> BufferElementLayouts()
    {
        List<StructLayout> layouts = [];

        foreach (uint pointerTypeId in _variablePointerTypes)
        {
            if (!_types.TryGetValue(pointerTypeId, out SpirvType? pointer) || pointer.Class != SpirvTypeClass.Pointer)
                continue;

            uint pointeeId = pointer.ElementId;

            if (!_structMembers.TryGetValue(pointeeId, out uint[]? members) || members.Length != 1)
                continue;

            uint innerId = members[0];

            if (!_types.TryGetValue(innerId, out SpirvType? inner))
                continue;

            uint elementId;
            int? stride = null;

            if (inner.Class is SpirvTypeClass.Array or SpirvTypeClass.RuntimeArray)
            {
                elementId = inner.ElementId;

                if (_arrayStrides.TryGetValue(innerId, out int arrayStride))
                    stride = arrayStride;
            }
            else if (inner.Class == SpirvTypeClass.Struct)
            {
                elementId = innerId;
            }
            else
            {
                continue;
            }

            if (!_structMembers.TryGetValue(elementId, out uint[]? elementMembers))
                continue;

            if (!_names.TryGetValue(elementId, out string? name) || name.Length == 0)
                continue;

            if (!_memberOffsets.TryGetValue(elementId, out Dictionary<int, int>? offsets))
                continue;

            int[] ordered = new int[elementMembers.Length];

            for (int i = 0; i < ordered.Length; i++)
            {
                if (!offsets.TryGetValue(i, out int offset))
                    throw new ToolException($"SPIR-V struct '{name}' member {i} has no Offset decoration.");

                ordered[i] = offset;
            }

            int end = 0;

            for (int i = 0; i < ordered.Length; i++)
                end = Math.Max(end, ordered[i] + SizeOf(elementMembers[i]));

            layouts.Add(new StructLayout
            {
                Name = name,
                Offsets = ordered,
                Size = stride ?? RoundUp16(end),
            });
        }

        return layouts;
    }

    private int SizeOf(uint typeId)
    {
        if (!_types.TryGetValue(typeId, out SpirvType? type))
            return 0;

        switch (type.Class)
        {
            case SpirvTypeClass.Scalar:
                return Math.Max(1, type.Width / 8);
            case SpirvTypeClass.Vector:
                return SizeOf(type.ComponentId) * (int)type.Count;
            case SpirvTypeClass.Matrix:
                return MatrixSize(typeId, type);
            case SpirvTypeClass.Array:
                return ArraySize(typeId, type);
            case SpirvTypeClass.Struct:
                return StructSize(typeId);
            default:
                return 0;
        }
    }

    private int MatrixSize(uint typeId, SpirvType type)
    {
        if (_matrixStrides.TryGetValue(typeId, out int stride))
            return stride * (int)type.Count;

        return SizeOf(type.ComponentId) * (int)type.Count;
    }

    private int ArraySize(uint typeId, SpirvType type)
    {
        if (_arrayStrides.TryGetValue(typeId, out int stride))
        {
            ulong length = LengthOf(type.Count);

            if (length > 0)
                return stride * (int)length;
        }

        return SizeOf(type.ElementId);
    }

    private ulong LengthOf(uint lengthId) => _constants.GetValueOrDefault(lengthId);

    private int StructSize(uint structId)
    {
        if (_structSizes.TryGetValue(structId, out int cached))
            return cached;

        if (!_structMembers.TryGetValue(structId, out uint[]? members) || !_memberOffsets.TryGetValue(structId, out Dictionary<int, int>? offsets))
            return 0;

        int end = 0;

        for (int i = 0; i < members.Length; i++)
        {
            if (offsets.TryGetValue(i, out int offset))
                end = Math.Max(end, offset + SizeOf(members[i]));
        }

        int size = RoundUp16(end);
        _structSizes[structId] = size;
        return size;
    }

    private static int RoundUp16(int value) => (value + 15) & ~15;

    private void Read(ReadOnlySpan<uint> words)
    {
        for (int i = 5; i < words.Length;)
        {
            uint instruction = words[i];
            int wordCount = (int)(instruction >> 16);
            ushort opcode = (ushort)(instruction & 0xFFFF);

            if (wordCount <= 0)
                break;

            switch (opcode)
            {
                case 5:
                {
                    uint target = words[i + 1];
                    _names[target] = ReadString(words.Slice(i + 2, wordCount - 2));
                    break;
                }
                case 71:
                {
                    uint target = words[i + 1];
                    uint decoration = words[i + 2];

                    if (decoration == 6)
                        _arrayStrides[target] = (int)words[i + 3];

                    break;
                }
                case 72:
                {
                    uint target = words[i + 1];
                    int member = (int)words[i + 2];
                    uint decoration = words[i + 3];
                    int value = (int)words[i + 4];

                    if (decoration == 35)
                    {
                        if (!_memberOffsets.TryGetValue(target, out Dictionary<int, int>? offsets))
                            _memberOffsets[target] = offsets = [];

                        offsets[member] = value;
                    }
                    else if (decoration == 7)
                    {
                        _matrixStrides[target] = value;
                    }

                    break;
                }
                case 21:
                {
                    _types[words[i + 1]] = new SpirvType { Class = SpirvTypeClass.Scalar, Width = (int)words[i + 2] };
                    break;
                }
                case 22:
                {
                    _types[words[i + 1]] = new SpirvType { Class = SpirvTypeClass.Scalar, Width = (int)words[i + 2] };
                    break;
                }
                case 20:
                {
                    _types[words[i + 1]] = new SpirvType { Class = SpirvTypeClass.Scalar, Width = 32 };
                    break;
                }
                case 23:
                {
                    _types[words[i + 1]] = new SpirvType
                    {
                        Class = SpirvTypeClass.Vector,
                        ComponentId = words[i + 2],
                        Count = words[i + 3],
                    };
                    break;
                }
                case 24:
                {
                    _types[words[i + 1]] = new SpirvType
                    {
                        Class = SpirvTypeClass.Matrix,
                        ComponentId = words[i + 2],
                        Count = words[i + 3],
                    };
                    break;
                }
                case 28:
                {
                    _types[words[i + 1]] = new SpirvType
                    {
                        Class = SpirvTypeClass.Array,
                        ElementId = words[i + 2],
                        Count = words[i + 3],
                    };
                    break;
                }
                case 29:
                {
                    _types[words[i + 1]] = new SpirvType
                    {
                        Class = SpirvTypeClass.RuntimeArray,
                        ElementId = words[i + 2],
                    };
                    break;
                }
                case 30:
                {
                    uint id = words[i + 1];
                    _types[id] = new SpirvType { Class = SpirvTypeClass.Struct };
                    _structMembers[id] = words.Slice(i + 2, wordCount - 2).ToArray();
                    break;
                }
                case 32:
                {
                    _types[words[i + 1]] = new SpirvType
                    {
                        Class = SpirvTypeClass.Pointer,
                        ElementId = words[i + 3],
                    };
                    break;
                }
                case 43:
                {
                    uint constantType = words[i + 1];
                    uint id = words[i + 2];

                    if (_types.TryGetValue(constantType, out SpirvType? type) && type.Class == SpirvTypeClass.Scalar)
                        _constants[id] = words[i + 3];

                    break;
                }
                case 59:
                {
                    _variablePointerTypes.Add(words[i + 1]);
                    break;
                }
            }

            i += wordCount;
        }
    }

    private static string ReadString(ReadOnlySpan<uint> words)
    {
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(words);
        int terminator = bytes.IndexOf((byte)0);

        if (terminator < 0)
            terminator = bytes.Length;

        return Encoding.UTF8.GetString(bytes[..terminator]);
    }
}
