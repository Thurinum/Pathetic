module Accumulation;

public struct Accumulator
{
    public float3 S;
    public uint n;

    public __init()
    {
        S = float3(0.0);
        n = 0;
    }

    [mutating]
    public void Add(float3 C)
    {
        S += C;
        n += 1;
    }

    public float3 Average()
    {
        return n > 0 ? S / float(n) : float3(0.0);
    }
}
