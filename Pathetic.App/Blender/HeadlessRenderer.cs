using System.Numerics;
using System.Runtime.InteropServices;
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
/// Renders a Blender-exported scene without a window and captures Pathetic's actual present
/// texture. This is the executable side of the Blender render-engine bridge.
/// </summary>
public sealed class HeadlessRenderer(
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
    SlangCompiler slangCompiler)
{
    public void Validate(BlenderRenderRequest request)
    {
        PopulateScene(request);
        sceneBvh.Update();

        Console.WriteLine(
            $"Validated scene: {spheres.Count} spheres, {planes.Count} planes, " +
            $"{triangles.Count} triangles, {sceneBvh.NodeCount} BVH nodes, " +
            $"{sceneBvh.PrimitiveRefCount} BVH primitives.");
    }

    public void Render(BlenderRenderRequest request, string outputPath)
    {
        if (request.Width == 0 || request.Height == 0)
            throw new ArgumentException("Render width and height must be greater than zero.");

        string? directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (directory is not null)
            Directory.CreateDirectory(directory);

        PopulateScene(request);

        uint totalSamples = Math.Max(request.Samples, 1);
        uint samplesPerPixel = Math.Clamp(request.SamplesPerPixel, 1, Math.Min(totalSamples, 4096u));
        pathTracerOptions.Value.SamplesPerPixel = samplesPerPixel;
        pathTracerOptions.Value.MaxBounces = Math.Clamp(request.MaxBounces, 1, 67);

        temporalAccumulationOptions.Value.Enabled = true;

        if (request.Tonemapper is not null
            && Enum.TryParse(request.Tonemapper, ignoreCase: true, out Tonemapper tonemapper))
        {
            tonemappingOptions.Value.Tonemapper = tonemapper;
        }

        tonemappingOptions.Value.Exposure = request.Exposure;

        // Build/upload the BVH before the graph allocates and binds its resources.
        sceneBvh.Update();

        GraphicsDeviceOptions deviceOptions = new()
        {
            PreferStandardClipSpaceYDirection = true,
            PreferDepthRangeZeroToOne = true,
            SyncToVerticalBlank = false,
        };

        using GraphicsDevice device = GraphicsDevice.CreateVulkan(deviceOptions);
        using CommandList commandList = device.ResourceFactory.CreateCommandList();

        using RenderGraph graph = graphFactory.Create(
            graphSpec.Value,
            device,
            request.Width,
            request.Height,
            PixelFormat.B8_G8_R8_A8_UNorm);

        uint frameCount = (totalSamples + samplesPerPixel - 1) / samplesPerPixel;

        for (uint frame = 0; frame < frameCount; frame++)
        {
            commandList.Begin();
            graph.Render(
                device,
                commandList,
                1.0f / 60.0f,
                request.Width,
                request.Height);
            commandList.End();
            device.SubmitCommands(commandList);
            device.WaitForIdle();
        }

        byte[] rgba = ReadPresentTexture(device, commandList, graph.PresentTexture, slangCompiler);

        if (outputPath.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
            WriteBmp(outputPath, rgba, request.Width, request.Height);
        else
            WriteRaw(outputPath, rgba, request.Width, request.Height);
    }

    private void PopulateScene(BlenderRenderRequest request)
    {
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
    }

    private static byte[] ReadPresentTexture(
        GraphicsDevice device,
        CommandList commandList,
        Texture presentTexture,
        SlangCompiler slangCompiler)
    {
        uint width = presentTexture.Width;
        uint height = presentTexture.Height;
        uint byteSize = checked(width * height * 4);

        ResourceFactory factory = device.ResourceFactory;

        using DeviceBuffer storage = factory.CreateBuffer(new BufferDescription(
            byteSize,
            BufferUsage.StructuredBufferReadWrite,
            4));

        using DeviceBuffer staging = factory.CreateBuffer(new BufferDescription(
            byteSize,
            BufferUsage.Staging));

        ShaderCompilationResult compiled = slangCompiler.CompileComputeShader("Readback");
        using Shader shader = factory.CreateShader(
            new ShaderDescription(ShaderStages.Compute, compiled.Code, SlangCompiler.EntryPointName));

        ResourceLayoutElementDescription[] bindings = compiled.Reflection
            .Select(binding => new ResourceLayoutElementDescription(binding.Name, binding.Kind, ShaderStages.Compute))
            .ToArray();

        using ResourceLayout layout = factory.CreateResourceLayout(new ResourceLayoutDescription(bindings));
        using Pipeline pipeline = factory.CreateComputePipeline(new ComputePipelineDescription(
            shader,
            layout,
            compiled.GroupSize.X,
            compiled.GroupSize.Y,
            compiled.GroupSize.Z));

        using ResourceSet resourceSet = factory.CreateResourceSet(new ResourceSetDescription(
            layout,
            presentTexture,
            storage));

        commandList.Begin();
        commandList.SetPipeline(pipeline);
        commandList.SetComputeResourceSet(0, resourceSet);
        commandList.Dispatch(
            (width + compiled.GroupSize.X - 1) / compiled.GroupSize.X,
            (height + compiled.GroupSize.Y - 1) / compiled.GroupSize.Y,
            1);
        commandList.CopyBuffer(storage, 0, staging, 0, byteSize);
        commandList.End();
        device.SubmitCommands(commandList);
        device.WaitForIdle();

        MappedResource mapped = device.Map(staging, MapMode.Read);
        byte[] rgba = new byte[byteSize];

        try
        {
            Marshal.Copy(mapped.Data, rgba, 0, (int)byteSize);
        }
        finally
        {
            device.Unmap(staging);
        }

        return rgba;
    }

    private static void WriteRaw(string path, byte[] rgba, uint width, uint height)
    {
        using FileStream stream = File.Create(path);
        using BinaryWriter writer = new(stream);
        writer.Write(width);
        writer.Write(height);
        writer.Write(rgba);
    }

    private static void WriteBmp(string path, byte[] rgba, uint width, uint height)
    {
        int rowSize = checked((int)(width * 4));
        int pixelDataSize = checked(rowSize * (int)height);
        int fileSize = 54 + pixelDataSize;

        using FileStream stream = File.Create(path);
        using BinaryWriter writer = new(stream);

        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(fileSize);
        writer.Write(0);
        writer.Write(54);
        writer.Write(40);
        writer.Write((int)width);
        writer.Write((int)height);
        writer.Write((short)1);
        writer.Write((short)32);
        writer.Write(0);
        writer.Write(pixelDataSize);
        writer.Write(2835);
        writer.Write(2835);
        writer.Write(0);
        writer.Write(0);

        byte[] row = new byte[rowSize];

        for (int y = (int)height - 1; y >= 0; y--)
        {
            int source = y * rowSize;
            for (int x = 0; x < (int)width; x++)
            {
                int index = source + x * 4;
                int destination = x * 4;
                row[destination + 0] = rgba[index + 2];
                row[destination + 1] = rgba[index + 1];
                row[destination + 2] = rgba[index + 0];
                row[destination + 3] = rgba[index + 3];
            }

            writer.Write(row);
        }
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
