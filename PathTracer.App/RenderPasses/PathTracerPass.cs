using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeoVeldrid;
using PathTracerApp.Components;
using PathTracerCore;
using PathTracerCore.Bvh;
using PathTracerCore.Renderer;
using PathTracerCore.Renderer.Camera;
using PathTracerCore.Renderer.Passes;
using PathTracerCore.Renderer.Primitives;
using PathTracerCore.Renderer.Shaders;

namespace PathTracerApp.RenderPasses;

public class PathTracerPass(
    CameraState view,
    ILogger<PathTracerPass> logger,
    SlangCompiler compiler,
    IOptions<EngineOptions> options,
    PrimitiveRegistry primitives,
    SceneBvh sceneBvh
    ) : RenderPass(compiler, options)
{
    private struct Params
    {
        public uint PlaneCount;
        public uint SphereCount;
        public uint Seed;
        public uint SamplesPerPixel;
        public uint MaxBounces;
        public uint BvhNodeCount;
    }

    protected override string ShaderModule => "PathTracer";

    public override string[] Inputs { get; } = [nameof(CirclesPass)];

    private uint _seed;
    private Sampler _sampler = null!;
    private DeviceBuffer _cameraBuffer = null!;
    private DeviceBuffer _planesBuffer = null!;
    private DeviceBuffer _spheresBuffer = null!;
    private DeviceBuffer _paramsBuffer = null!;
    private PrimitiveBuffer<PlanePrimitive> _planes = null!;
    private PrimitiveBuffer<SpherePrimitive> _spheres = null!;
    private int _bufferVersionPlanes = 0;
    private int _bufferVersionSpheres = 0;
    
    private DeviceBuffer _bvhNodesBuffer = null!;
    private DeviceBuffer _bvhPrimitiveRefsBuffer = null!;
    private BvhGpuData _bvhData = new([], []);
    
    protected override void SetupResources(GraphicsDevice device)
    {
        _planes = primitives.Get<PlanePrimitive>();
        _spheres = primitives.Get<SpherePrimitive>();

        _sampler = device.ResourceFactory.CreateSampler(SamplerDescription.Linear);
        RegisterInput("circles", nameof(CirclesPass));
        Register("circlesSample", ResourceKind.Sampler, _sampler);

        BufferDescription cameraDesc = new()
        {
            SizeInBytes = (uint)Unsafe.SizeOf<CameraData>(),
            Usage = BufferUsage.UniformBuffer
        };
        _cameraBuffer = device.ResourceFactory.CreateBuffer(cameraDesc);
        Register("camera", ResourceKind.UniformBuffer, _cameraBuffer);
        
        BuildPrimitivesBuffers(device);
        Register("planes", ResourceKind.StructuredBufferReadOnly, _planesBuffer);
        Register("spheres", ResourceKind.StructuredBufferReadOnly, _spheresBuffer);

        BufferDescription paramsBufferDesc = new()
        {
            SizeInBytes = 32,
            Usage = BufferUsage.UniformBuffer
        };
        _paramsBuffer = device.ResourceFactory.CreateBuffer(paramsBufferDesc);
        Register("params", ResourceKind.UniformBuffer, _paramsBuffer);
        
        BuildBvhBuffers(device);
        Register("bvhNodes", ResourceKind.StructuredBufferReadOnly, _bvhNodesBuffer);
        Register("bvhPrimitiveRefs", ResourceKind.StructuredBufferReadOnly, _bvhPrimitiveRefsBuffer);
    }
    
    private void BuildBvhBuffers(GraphicsDevice device)
    {
        var bvhNodesDesc = new BufferDescription
        {
            SizeInBytes = (uint)(_bvhData.Nodes.Length * Unsafe.SizeOf<GpuBvhNode>()),
            StructureByteStride = (uint)Unsafe.SizeOf<GpuBvhNode>(),
            Usage = BufferUsage.StructuredBufferReadOnly | BufferUsage.Dynamic
        };
        _bvhNodesBuffer = device.ResourceFactory.CreateBuffer(bvhNodesDesc);

        var bvhPrimitiveRefsDesc = new BufferDescription
        {
            SizeInBytes = (uint)(_bvhData.PrimitiveRefs.Length * Unsafe.SizeOf<GpuBvhPrimitiveRef>()),
            StructureByteStride = (uint)Unsafe.SizeOf<GpuBvhPrimitiveRef>(),
            Usage = BufferUsage.StructuredBufferReadOnly | BufferUsage.Dynamic
        };
        _bvhPrimitiveRefsBuffer = device.ResourceFactory.CreateBuffer(bvhPrimitiveRefsDesc);
    }
    
    private void RebuildBvhBuffers(GraphicsDevice device)
    {
        _bvhNodesBuffer?.Dispose();
        _bvhPrimitiveRefsBuffer?.Dispose();
        BuildBvhBuffers(device);
        UpdateBinding("bvhNodes", _bvhNodesBuffer!, device);
        UpdateBinding("bvhPrimitiveRefs", _bvhPrimitiveRefsBuffer!, device);
    }

    private void BuildPrimitivesBuffers(GraphicsDevice device)
    {
        BufferDescription planesBufferDesc = new()
        {
            SizeInBytes = _planes.Stride * (uint)_planes.Capacity,
            StructureByteStride = _planes.Stride,
            Usage = BufferUsage.StructuredBufferReadOnly | BufferUsage.Dynamic
        };
        _planesBuffer = device.ResourceFactory.CreateBuffer(planesBufferDesc);
        
        BufferDescription spheresBufferDesc = new()
        {
            SizeInBytes = _spheres.Stride * (uint)_spheres.Capacity,
            StructureByteStride = _spheres.Stride,
            Usage = BufferUsage.StructuredBufferReadOnly | BufferUsage.Dynamic
        };
        _spheresBuffer = device.ResourceFactory.CreateBuffer(spheresBufferDesc);
    }

    private void RebuildPrimitivesBuffer(GraphicsDevice device)
    {
        _planesBuffer?.Dispose();
        _spheresBuffer?.Dispose();
        BuildPrimitivesBuffers(device);
        UpdateBinding("planes", _planesBuffer!, device);
        UpdateBinding("spheres", _spheresBuffer!, device);
    }
    
    protected override void Upload(FrameContext ctx)
    {
        UploadPlanes(ctx);
        UploadSpheres(ctx);

        if (sceneBvh.IsDirty)
        {
            sceneBvh.Rebuild();
            _bvhData = sceneBvh.Flatten();
            UploadBvh(ctx);
        }

        Params @params = new()
        {
            PlaneCount = (uint)_planes.PathTracerPass,
            SphereCount = (uint)_spheres.PathTracerPass,
            Seed = _seed++,
            SamplesPerPixel = options.Value.SamplesPerPixel,
            MaxBounces = options.Value.MaxBounces,
            BvhNodeCount = (uint)_bvhData.Nodes.Length
        };
        ctx.Cmd.UpdateBuffer(_paramsBuffer, 0, @params);

        ctx.Cmd.UpdateBuffer(_cameraBuffer, 0, view.Data);
    }

    private void UploadBvh(FrameContext ctx)
    {
        RebuildBvhBuffers(ctx.Device);
        ctx.Cmd.UpdateBuffer(_bvhNodesBuffer, 0, _bvhData.Nodes);
        ctx.Cmd.UpdateBuffer(_bvhPrimitiveRefsBuffer, 0, _bvhData.PrimitiveRefs);
    }

    private void UploadPlanes(FrameContext ctx)
    {
        if (_bufferVersionPlanes != _planes.CapacityVersion)
        {
            ctx.Device.WaitForIdle();
            RebuildPrimitivesBuffer(ctx.Device);
            _bufferVersionPlanes = _planes.CapacityVersion;
            _planes.MarkAllDirty();
            logger.LogInformation($"Primitives buffer was rebuilt to {_planesBuffer.SizeInBytes} bytes."); 
        }
        
        for (int i = 0; i < _planes.PathTracerPass; i++)
        {
            if (_planes.IsDirty(i))
            {
                ctx.Cmd.UpdateBuffer(_planesBuffer, (uint)i * _planes.Stride, _planes.Data[i]);
            }
        }
        _planes.ClearDirty();
    }
    
    private void UploadSpheres(FrameContext ctx)
    {
        if (_bufferVersionSpheres != _spheres.CapacityVersion)
        {
            ctx.Device.WaitForIdle();
            RebuildPrimitivesBuffer(ctx.Device);
            _bufferVersionSpheres = _spheres.CapacityVersion;
            _spheres.MarkAllDirty();
            logger.LogInformation($"Primitives buffer was rebuilt to {_spheresBuffer.SizeInBytes} bytes."); 
        }
        
        for (int i = 0; i < _spheres.PathTracerPass; i++)
        {
            if (_spheres.IsDirty(i))
            {
                ctx.Cmd.UpdateBuffer(_spheresBuffer, (uint)i * _spheres.Stride, _spheres.Data[i]);
            }
        }
        _spheres.ClearDirty();
    }
    
    protected override void DisposeResources()
    {
        _sampler.Dispose();
        _planesBuffer.Dispose();
        _spheresBuffer.Dispose();
        _paramsBuffer.Dispose();
        _cameraBuffer.Dispose();
        _bvhNodesBuffer.Dispose();
        _bvhPrimitiveRefsBuffer.Dispose();
    }
}