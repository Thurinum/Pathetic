module FrameState;

[Flags]
public enum SceneChange
{
    None = 0,
    Structure = 1 << 0,
    Content = 1 << 1,
    Allocation = 1 << 2,
    Parameters = 1 << 3,
}

public struct FrameState
{
    public uint FrameIndex;
    public float DeltaTime;
    public SceneChange SceneRevision;
}

public ConstantBuffer<FrameState> frameState;
