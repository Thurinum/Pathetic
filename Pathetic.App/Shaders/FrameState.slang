module FrameState;

public struct FrameState
{
    public uint FrameIndex;
    public float DeltaTime;
}

public ConstantBuffer<FrameState> frameState;
