using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;
using SlangGen = Pathetic.Generated.Slang;

namespace Pathetic.Tests;

public sealed class LayoutTests
{
    [Fact]
    public void PrimitiveLayoutsMatchGpuBuffers()
    {
        Assert.Equal(32, Unsafe.SizeOf<SlangGen.Primitives.Sphere>());
        Assert.Equal(12, Marshal.OffsetOf<SlangGen.Primitives.Sphere>(nameof(SlangGen.Primitives.Sphere.Radius)).ToInt32());
        Assert.Equal(16, Marshal.OffsetOf<SlangGen.Primitives.Sphere>(nameof(SlangGen.Primitives.Sphere.MaterialId)).ToInt32());

        Assert.Equal(48, Unsafe.SizeOf<SlangGen.Primitives.Plane>());
        Assert.Equal(16, Marshal.OffsetOf<SlangGen.Primitives.Plane>(nameof(SlangGen.Primitives.Plane.U)).ToInt32());
        Assert.Equal(32, Marshal.OffsetOf<SlangGen.Primitives.Plane>(nameof(SlangGen.Primitives.Plane.V)).ToInt32());
        Assert.Equal(44, Marshal.OffsetOf<SlangGen.Primitives.Plane>(nameof(SlangGen.Primitives.Plane.MaterialId)).ToInt32());

        Assert.Equal(112, Unsafe.SizeOf<SlangGen.Primitives.Triangle>());
        Assert.Equal(48, Marshal.OffsetOf<SlangGen.Primitives.Triangle>(nameof(SlangGen.Primitives.Triangle.N0)).ToInt32());
        Assert.Equal(92, Marshal.OffsetOf<SlangGen.Primitives.Triangle>(nameof(SlangGen.Primitives.Triangle.MaterialId)).ToInt32());
        Assert.Equal(96, Marshal.OffsetOf<SlangGen.Primitives.Triangle>(nameof(SlangGen.Primitives.Triangle.TransformId)).ToInt32());

        Assert.Equal(176, Unsafe.SizeOf<SlangGen.Primitives.MeshTransform>());
        Assert.Equal(160, Marshal.OffsetOf<SlangGen.Primitives.MeshTransform>(nameof(SlangGen.Primitives.MeshTransform.ForwardOffset)).ToInt32());
    }

    [Fact]
    public void UniformLayoutsMatchGpuBuffers()
    {
        Assert.Equal(64, Unsafe.SizeOf<SlangGen.Camera.Camera>());
        Assert.Equal(28, Marshal.OffsetOf<SlangGen.Camera.Camera>(nameof(SlangGen.Camera.Camera.TanHalfFov)).ToInt32());
        Assert.Equal(60, Marshal.OffsetOf<SlangGen.Camera.Camera>(nameof(SlangGen.Camera.Camera.ApertureRadius)).ToInt32());

        Assert.Equal(12, Unsafe.SizeOf<SlangGen.FrameState.FrameState>());
        Assert.Equal(4, Marshal.OffsetOf<SlangGen.FrameState.FrameState>(nameof(SlangGen.FrameState.FrameState.DeltaTime)).ToInt32());
        Assert.Equal(8, Marshal.OffsetOf<SlangGen.FrameState.FrameState>(nameof(SlangGen.FrameState.FrameState.SceneRevision)).ToInt32());

        Assert.Equal(4, Unsafe.SizeOf<SlangGen.Circles.Params>());
        Assert.Equal(16, Unsafe.SizeOf<SlangGen.TemporalAccumulation.Params>());
        Assert.Equal(8, Marshal.OffsetOf<SlangGen.TemporalAccumulation.Params>(nameof(SlangGen.TemporalAccumulation.Params.Exposure)).ToInt32());
        Assert.Equal(32, Unsafe.SizeOf<SlangGen.PathTracer.Params>());
        Assert.Equal(28, Marshal.OffsetOf<SlangGen.PathTracer.Params>(nameof(SlangGen.PathTracer.Params.SmoothShading)).ToInt32());
    }

    [Fact]
    public void BvhLayoutsMatchGpuBuffers()
    {
        Assert.Equal(48, Unsafe.SizeOf<SlangGen.Bvh.BvhNode>());
        Assert.Equal(16, Marshal.OffsetOf<SlangGen.Bvh.BvhNode>(nameof(SlangGen.Bvh.BvhNode.Max)).ToInt32());
        Assert.Equal(28, Marshal.OffsetOf<SlangGen.Bvh.BvhNode>(nameof(SlangGen.Bvh.BvhNode.Left)).ToInt32());
        Assert.Equal(40, Marshal.OffsetOf<SlangGen.Bvh.BvhNode>(nameof(SlangGen.Bvh.BvhNode.PrimitiveCount)).ToInt32());

        Assert.Equal(8, Unsafe.SizeOf<SlangGen.Bvh.BvhPrimitiveRef>());
        Assert.Equal(4, Marshal.OffsetOf<SlangGen.Bvh.BvhPrimitiveRef>(nameof(SlangGen.Bvh.BvhPrimitiveRef.Index)).ToInt32());
    }
}
