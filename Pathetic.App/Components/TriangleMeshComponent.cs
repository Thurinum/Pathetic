using System.Numerics;
using System.Runtime.InteropServices;
using NeoVeldrid;
using Pathetic.Renderer.Buffers;
using Pathetic.SceneGraph.Components;

namespace Pathetic.App.Components;

/// <summary>
/// A single triangle. The BVH owns the acceleration structure; this is the flat primitive
/// payload consumed by the path tracer.
/// </summary>
// Matches the std430 layout Slang uses for a StructuredBuffer element. A float3 is
// 16-byte aligned, so the vertices are at 0, 16 and 32 rather than tightly packed.
[StructLayout(LayoutKind.Explicit, Size = 64)]
public record struct TrianglePrimitive
{
    [FieldOffset(00)] public Vector3 V0;
    [FieldOffset(16)] public Vector3 V1;
    [FieldOffset(32)] public Vector3 V2;
    [FieldOffset(48)] public RgbaByte Albedo;
    [FieldOffset(52)] public RgbaByte Emission;
}

/// <summary>
/// Adds a list of triangles to the shared primitive buffer. Used by the Blender bridge for
/// evaluated meshes and kept independent from any editor-only scene graph features.
/// </summary>
public sealed class TriangleMeshComponent(SparseSet<TrianglePrimitive> primitives) : Component
{
    public List<TrianglePrimitive> Triangles { get; } = [];

    private readonly List<Handle> _handles = [];

    protected override void Setup()
    {
        foreach (TrianglePrimitive triangle in Triangles)
            _handles.Add(primitives.Add(triangle));
    }

    protected override void Cleanup()
    {
        foreach (Handle handle in _handles)
        {
            Handle current = handle;
            primitives.Remove(ref current);
        }

        _handles.Clear();
    }
}
