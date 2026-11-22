using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pathetic.App;
using Pathetic.App.Assets;
using Pathetic.App.Bvh;
using Pathetic.App.Components;
using Pathetic.App.Components.Camera;
using Pathetic.App.Input;
using Pathetic.App.RenderPasses;
using Pathetic.Input;
using Pathetic.Renderer;
using Pathetic.Renderer.Bvh;
using Pathetic.Renderer.DI;
using Pathetic.Renderer.Resources;
using Pathetic.SceneGraph.DI;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

string iniPath = Path.Combine(AppContext.BaseDirectory, "Pathetic.ini");
builder.Configuration.AddIniFile(iniPath, optional: false, reloadOnChange: false);

AddOptions<RendererOptions>();
AddOptions<PathTracerOptions>();
AddOptions<TemporalAccumulationOptions>();
AddOptions<TonemappingOptions>();
AddOptions<CameraOptions>();

builder.Services.AddSingleton<RenderDebugSettings>();
builder.Services.AddSingleton<ConvergenceController>();

builder.Services.AddRenderer(graph => graph
    // .AddPass<CirclesPass>()
    .AddPass<PathTracerPass>()
    .AddPass<TemporalAccumulationPass>()
    .Present(GPUType.Resources.TemporalAccumulation.color));

builder.Services.AddStructuredBuffer<GPUType.Sphere>(GPUType.Resources.PathTracer.spheres, sparseStorage: true);
builder.Services.AddStructuredBuffer<GPUType.Plane>(GPUType.Resources.PathTracer.planes, sparseStorage: true);
builder.Services.AddStructuredBuffer<GPUType.Triangle>(GPUType.Resources.PathTracer.triangles, sparseStorage: true);
builder.Services.AddStructuredBuffer<GPUType.MeshTransform>(GPUType.Resources.Primitives.meshTransforms);
builder.Services.AddStructuredBuffer<Material>(GPUType.Resources.PathTracer.materials);
builder.Services.AddSingleton<MaterialLibrary>();
builder.Services.AddSingleton<MeshTransformLibrary>();
builder.Services.AddSingleton<ObjLoader>();
builder.Services.AddStructuredBuffer<CameraProvider>();

builder.Services.AddSingleton<SceneBvh>();
builder.Services.AddSingleton(sp => new BvhBufferUploader<GPUType.BvhNode>(
    GPUType.Resources.PathTracer.bvhNodes,
    sp.GetRequiredService<SceneBvh>(),
    scene => scene.Nodes));
builder.Services.AddSingleton<IResourceUploader>(sp => sp.GetRequiredService<BvhBufferUploader<GPUType.BvhNode>>());
builder.Services.AddSingleton(sp => new BvhBufferUploader<GPUType.BvhPrimitiveRef>(
    GPUType.Resources.PathTracer.bvhPrimitiveRefs,
    sp.GetRequiredService<SceneBvh>(),
    scene => scene.PrimitiveRefs));
builder.Services.AddSingleton<IResourceUploader>(sp => sp.GetRequiredService<BvhBufferUploader<GPUType.BvhPrimitiveRef>>());

builder.Services.AddSceneGraph();

var inputActions = new InputActions();
builder.Services.AddSingleton(inputActions);
builder.Services.AddInputSystem(inputActions.All);

builder.Services.AddSingleton<App>(); 

using var host = builder.Build();   

await host.StartAsync();                             
host.Services.GetRequiredService<App>().Run();       
await host.StopAsync();
return;

void AddOptions<T>() where T : class, IConfig
{
    builder.Services.AddOptions<T>()
        .BindConfiguration(T.Section)
        .ValidateDataAnnotations()
        .ValidateOnStart();
}