using System.Numerics;
using Microsoft.Extensions.Options;
using NeoVeldrid;
using Pathetic.App.Bvh;
using Pathetic.App.Components;
using Pathetic.App.Components.Camera;
using Pathetic.App.RenderPasses;
using Pathetic.Renderer.Buffers;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Shaders;

namespace Pathetic.App.Blender;

/// <summary>
/// A persistent offscreen Pathetic renderer. Blender keeps this session alive so the
/// Vulkan device and shaders are created once, then camera changes and frames are cheap.
/// </summary>
public sealed class RenderSession(
    RenderGraphFactory graphFactory,
    IOptions<RenderGraphSpec> graphSpec,
    StructuredBuffer<SpherePrimitive> spheres,
    StructuredBuffer<PlanePrimitive> planes,
    StructuredBuffer<TrianglePrimitive> triangles,
    CameraProvider cameraProvider,
    SceneBvh sceneBvh,
    IOptions<PathTracerOptions> pathTracerOptions,
    IOptions<TemporalAccumulationOptions> temporalAccumulationOptions,
    IOptions<TonemappingOptions> tonemappingOptions,
    SlangCompiler slangCompiler) : IDisposable
{
    private GraphicsDevice? _device;
    private CommandList? _commandList;
    private RenderGraph? _graph;
    private ReadbackCapture? _capture;
    private uint _width;
    private uint _height;
    private uint _frameIndex;
    private bool _disposed;

    public bool IsLoaded { get; private set; }
    public uint Width => _width;
    public uint Height => _height;
    public uint FrameIndex => _frameIndex;

    public void LoadScene(BlenderRenderRequest request)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (request.Width == 0 || request.Height == 0)
            throw new ArgumentException("Render width and height must be greater than zero.");

        EnsureDevice();

        bool resize = _graph is null || request.Width != _width || request.Height != _height;
        if (resize)
        {
            _capture?.Dispose();
            _capture = null;
            _graph?.Dispose();
            _graph = null;
        }

        spheres.Clear();
        planes.Clear();
        triangles.Clear();

        foreach (SphereDto sphere in request.Spheres)
        {
            spheres.Add(new SpherePrimitive
            {
                Center = ToVector3(sphere.Center),
                Radius = sphere.Radius,
                Albedo = ToColor(sphere.Albedo),
                Emission = ToColor(sphere.Emission),
            });
        }

        foreach (PlaneDto plane in request.Planes)
        {
            planes.Add(new PlanePrimitive
            {
                Point = ToVector3(plane.Point),
                ExtentX = ToVector3(plane.ExtentX),
                ExtentY = ToVector3(plane.ExtentY),
            });
        }

        foreach (TriangleDto triangle in request.Triangles)
        {
            triangles.Add(new TrianglePrimitive
            {
                V0 = ToVector3(triangle.V0),
                V1 = ToVector3(triangle.V1),
                V2 = ToVector3(triangle.V2),
                Albedo = ToColor(triangle.Albedo),
                Emission = ToColor(triangle.Emission),
            });
        }

        CameraDto camera = request.Camera;
        cameraProvider.Set(new CameraData
        {
            Origin = ToVector3(camera.Origin),
            Right = ToVector3(camera.Right),
            Up = ToVector3(camera.Up),
            Forward = ToVector3(camera.Forward),
            TanHalfFOV = camera.TanHalfFov,
            FocusDistance = camera.FocusDistance,
            ApertureRadius = camera.ApertureRadius,
        });

        uint totalSamples = Math.Max(request.Samples, 1);
        pathTracerOptions.Value.SamplesPerPixel = Math.Clamp(
            request.SamplesPerPixel,
            1,
            Math.Min(totalSamples, 4096u));
        pathTracerOptions.Value.MaxBounces = Math.Clamp(request.MaxBounces, 1, 67);
        temporalAccumulationOptions.Value.Enabled = true;

        if (request.Tonemapper is not null
            && Enum.TryParse(request.Tonemapper, ignoreCase: true, out Tonemapper tonemapper))
        {
            tonemappingOptions.Value.Tonemapper = tonemapper;
        }

        tonemappingOptions.Value.Exposure = request.Exposure;

        sceneBvh.Update();

        _width = request.Width;
        _height = request.Height;

        if (_graph is null)
        {
            _graph = graphFactory.Create(
                graphSpec.Value,
                _device!,
                _width,
                _height,
                PixelFormat.B8_G8_R8_A8_UNorm);
            _capture = new ReadbackCapture(_device!, _graph.PresentTexture, slangCompiler);
        }

        IsLoaded = true;
    }

    public void UpdateCamera(CameraDto camera)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cameraProvider.Set(new CameraData
        {
            Origin = ToVector3(camera.Origin),
            Right = ToVector3(camera.Right),
            Up = ToVector3(camera.Up),
            Forward = ToVector3(camera.Forward),
            TanHalfFOV = camera.TanHalfFov,
            FocusDistance = camera.FocusDistance,
            ApertureRadius = camera.ApertureRadius,
        });
    }

    public void UpdateSettings(
        int? samplesPerPixel,
        int? maxBounces,
        string? tonemapperName,
        float? exposure)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (samplesPerPixel is > 0)
            pathTracerOptions.Value.SamplesPerPixel = (uint)Math.Min(samplesPerPixel.Value, 4096);

        if (maxBounces is > 0)
            pathTracerOptions.Value.MaxBounces = (uint)Math.Min(maxBounces.Value, 67);

        if (tonemapperName is not null
            && Enum.TryParse(tonemapperName, ignoreCase: true, out Tonemapper tonemapper))
        {
            tonemappingOptions.Value.Tonemapper = tonemapper;
        }

        if (exposure.HasValue)
            tonemappingOptions.Value.Exposure = exposure.Value;
    }

    public byte[] RenderFrame()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!IsLoaded || _graph is null || _commandList is null || _device is null || _capture is null)
            return [];

        _commandList.Begin();
        _graph.Render(
            _device,
            _commandList,
            1.0f / 60.0f,
            _width,
            _height);
        _commandList.End();
        _device.SubmitCommands(_commandList);
        _device.WaitForIdle();

        _frameIndex++;
        return _capture.Capture(_device, _commandList, _width, _height);
    }

    private void EnsureDevice()
    {
        if (_device is not null)
            return;

        GraphicsDeviceOptions options = new()
        {
            PreferStandardClipSpaceYDirection = true,
            PreferDepthRangeZeroToOne = true,
            SyncToVerticalBlank = false,
        };

        _device = GraphicsDevice.CreateVulkan(options);
        _commandList = _device.ResourceFactory.CreateCommandList();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _capture?.Dispose();
        _graph?.Dispose();
        _commandList?.Dispose();
        _device?.Dispose();
    }

    private static Vector3 ToVector3(List<float> values) =>
        new(
            values.Count > 0 ? values[0] : 0.0f,
            values.Count > 1 ? values[1] : 0.0f,
            values.Count > 2 ? values[2] : 0.0f);

    private static RgbaByte ToColor(List<byte> values) =>
        new(
            values.Count > 0 ? values[0] : (byte)128,
            values.Count > 1 ? values[1] : (byte)128,
            values.Count > 2 ? values[2] : (byte)128,
            values.Count > 3 ? values[3] : (byte)255);
}
