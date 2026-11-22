using System.Runtime.InteropServices;
using Silk.NET.SPIRV.Reflect;

namespace Pathetic.Slang.Tool;

internal sealed record StructLayout(string Name, int Size, int[] Offsets);

internal static unsafe class SpirvLayouts
{
    public static List<StructLayout> Read(byte[] code)
    {
        Reflect api = Reflect.GetApi();
        ReflectShaderModule module = default;
        Result result = api.GetShaderModule((nuint)code.Length, code, new Span<ReflectShaderModule>(&module, 1));

        if (result != Result.Success)
            throw new ToolException($"SPIRV-Reflect could not read the module: {result}.");

        try
        {
            List<StructLayout> layouts = [];

            for (int i = 0; i < module.DescriptorBindingCount; i++)
            {
                DescriptorBinding binding = module.DescriptorBindings[i];
                BlockVariable block = binding.Block;

                if (block.MemberCount == 0)
                    continue;

                BlockVariable element = binding.DescriptorType == DescriptorType.StorageBuffer && block.MemberCount == 1
                    ? block.Members[0]
                    : block;

                string? name = TypeName(element);

                if (name is null || element.MemberCount == 0)
                    continue;

                int[] offsets = new int[element.MemberCount];

                for (int member = 0; member < offsets.Length; member++)
                    offsets[member] = (int)element.Members[member].Offset;

                layouts.Add(new StructLayout(name, (int)element.PaddedSize, offsets));
            }

            return layouts;
        }
        finally
        {
            api.DestroyShaderModule(new Span<ReflectShaderModule>(&module, 1));
        }
    }

    private static string? TypeName(BlockVariable variable) =>
        variable.TypeDescription is null || variable.TypeDescription->TypeName is null
            ? null
            : Marshal.PtrToStringAnsi((IntPtr)variable.TypeDescription->TypeName);
}
