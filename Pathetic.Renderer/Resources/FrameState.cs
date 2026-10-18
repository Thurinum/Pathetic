using System.Runtime.InteropServices;

namespace Pathetic.Renderer.Resources;

[StructLayout(LayoutKind.Explicit, Size = 16)]
public struct FrameState
{
    [FieldOffset(0)] public uint FrameIndex;
    [FieldOffset(4)] public float DeltaTime;
    [FieldOffset(8)] public SceneChange SceneRevision;
}
