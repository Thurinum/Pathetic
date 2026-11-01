using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pathetic.App;
using Pathetic.App.Blender;
using Pathetic.App.Bvh;
using Pathetic.App.Components;
using Pathetic.App.Components.Camera;
using Pathetic.App.RenderPasses;
using Pathetic.Renderer;
using Pathetic.Renderer.DI;
using Pathetic.Renderer.Shaders;
using Pathetic.SceneGraph.DI;

bool headless = args.Any(arg => string.Equals(arg, "--headless", StringComparison.OrdinalIgnoreCase));
bool compileShaders = args.Any(arg => string.Equals(arg, "--compile-shaders", StringComparison.OrdinalIgnoreCase));
bool validateScene = args.Any(arg => string.Equals(arg, "--validate-scene", StringComparison.OrdinalIgnoreCase));
bool bridge = args.Any(arg => string.Equals(arg, "--bridge", StringComparison.OrdinalIgnoreCase));
string? scenePath = GetArgument(args, "--scene");
string? outputPath = GetArgument(args, "--output");

string[] hostArgs = headless || compileShaders || validateScene || bridge ? [] : args;
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = hostArgs,
    ContentRootPath = AppContext.BaseDirectory
});

if (bridge)
{
    // stdout is the binary frame stream. Logging must not contaminate it.
    builder.Logging.ClearProviders();
}

string iniPath = Path.Combine(AppContext.BaseDirectory, "Pathetic.ini");
builder.Configuration.AddIniFile(iniPath, optional: false, reloadOnChange: false);

AddOptions<RendererOptions>();
AddOptions<PathTracerOptions>();
AddOptions<TemporalAccumulationOptions>();
AddOptions<TonemappingOptions>();

builder.Services.AddRenderer(graph => graph
    // .AddPass<CirclesPass>()
    .AddPass<PathTracerPass>()
    .AddPass<TemporalAccumulationPass>()
    .Present(ResourceName.PresentColor));

builder.Services.AddStructuredBuffer<SpherePrimitive>(ResourceName.Spheres, sparseStorage: true);
builder.Services.AddStructuredBuffer<PlanePrimitive>(ResourceName.Planes, sparseStorage: true);
builder.Services.AddStructuredBuffer<TrianglePrimitive>(ResourceName.Triangles, sparseStorage: true);
builder.Services.AddStructuredBuffer<GpuBvhNode>(ResourceName.BvhNodes);
builder.Services.AddStructuredBuffer<GpuBvhPrimitiveRef>(ResourceName.BvhPrimitiveRefs);
// builder.Services.AddStructuredBuffer<CirclePrimitive>(ResourceName.Circles, sparseStorage: true);

builder.Services.AddStructuredBuffer<CameraProvider>();
builder.Services.AddSingleton<SceneBvh>();
builder.Services.AddSceneGraph();
builder.Services.AddSingleton<HeadlessRenderer>();
builder.Services.AddTransient<RenderSession>();
builder.Services.AddSingleton<BridgeRunner>();
builder.Services.AddSingleton<App>();

using var host = builder.Build();

await host.StartAsync();

try
{
    if (bridge)
    {
        host.Services.GetRequiredService<BridgeRunner>().Run();
    }
    else if (compileShaders)
    {
        SlangCompiler compiler = host.Services.GetRequiredService<SlangCompiler>();

        foreach (string module in new[] { "PathTracer", "TemporalAccumulation", "Readback" })
        {
            ShaderCompilationResult compiled = compiler.CompileComputeShader(module);
            string bindings = string.Join(
                ", ",
                compiled.Reflection.Select(binding => $"{binding.Name}:{binding.Kind}"));
            Console.WriteLine($"Compiled {module}: {bindings}");
        }
    }
    else if (validateScene)
    {
        if (scenePath is null)
            throw new ArgumentException("Scene validation requires --scene <path>.");

        BlenderRenderRequest request = BlenderRenderRequest.Load(scenePath);
        host.Services.GetRequiredService<HeadlessRenderer>().Validate(request);
    }
    else if (headless)
    {
        if (scenePath is null || outputPath is null)
            throw new ArgumentException("Headless mode requires --scene <path> and --output <path>.");

        BlenderRenderRequest request = BlenderRenderRequest.Load(scenePath);
        host.Services.GetRequiredService<HeadlessRenderer>().Render(request, outputPath);
    }
    else
    {
        host.Services.GetRequiredService<App>().Run();
    }
}
finally
{
    await host.StopAsync();
}

return;

void AddOptions<T>() where T : class, IConfig
{
    builder.Services.AddOptions<T>()
        .BindConfiguration(T.Section)
        .ValidateDataAnnotations()
        .ValidateOnStart();
}

static string? GetArgument(string[] arguments, string name)
{
    int index = Array.FindIndex(arguments, arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));
    return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
}