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
using Pathetic.App.Scenes;
using Pathetic.App.Scenes.Catalog;
using Pathetic.App.Scenes.DI;
using Pathetic.Input;
using Pathetic.Profiling.DI;
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
builder.Configuration.AddCommandLine(args);

AddOptions<RendererOptions>();
AddOptions<PathTracerOptions>();
AddOptions<TemporalAccumulationOptions>();
AddOptions<TonemappingOptions>();
AddOptions<SceneOptions>();
AddOptions<CameraOptions>();

builder.Services.AddSingleton<RenderDebugSettings>();
builder.Services.AddSingleton<ConvergenceController>();

builder.Services.AddRenderer(graph => graph
    // .AddPass<CirclesPass>()
    .AddPass<PathTracerPass>()
    .AddPass<TemporalAccumulationPass>()
    .Present(GPUTypes.Bindings.TemporalAccumulation.color));

builder.Services.AddStructuredBuffer<GPUTypes.Sphere>(GPUTypes.Bindings.PathTracer.spheres, sparseStorage: true);
builder.Services.AddStructuredBuffer<GPUTypes.Plane>(GPUTypes.Bindings.PathTracer.planes, sparseStorage: true);
builder.Services.AddStructuredBuffer<GPUTypes.Triangle>(GPUTypes.Bindings.PathTracer.triangles, sparseStorage: true);
builder.Services.AddStructuredBuffer<GPUTypes.MeshTransform>(GPUTypes.Bindings.Primitives.meshTransforms);
builder.Services.AddStructuredBuffer<Material>(GPUTypes.Bindings.PathTracer.materials);
builder.Services.AddSingleton<MaterialLibrary>();
builder.Services.AddSingleton<MeshTransformLibrary>();
builder.Services.AddSingleton<ObjLoader>();
builder.Services.AddStructuredBuffer<CameraProvider>();

builder.Services.AddSingleton<SceneBvh>();
builder.Services.AddSingleton(sp => new BvhBufferUploader<GPUTypes.BvhNode>(
    GPUTypes.Bindings.PathTracer.bvhNodes,
    sp.GetRequiredService<SceneBvh>(),
    scene => scene.Nodes));
builder.Services.AddSingleton<IResourceUploader>(sp => sp.GetRequiredService<BvhBufferUploader<GPUTypes.BvhNode>>());
builder.Services.AddSingleton(sp => new BvhBufferUploader<GPUTypes.BvhPrimitiveRef>(
    GPUTypes.Bindings.PathTracer.bvhPrimitiveRefs,
    sp.GetRequiredService<SceneBvh>(),
    scene => scene.PrimitiveRefs));
builder.Services.AddSingleton<IResourceUploader>(sp => sp.GetRequiredService<BvhBufferUploader<GPUTypes.BvhPrimitiveRef>>());

builder.Services.AddSceneGraph();
builder.Services.AddScene<DefaultScene>();
builder.Services.AddScene<OneWeekendScene>();
builder.Services.AddScene<SphereFieldScene>();
builder.Services.AddScene<SphereflakeScene>();
builder.Services.AddScene<CornellBoxScene>();
builder.Services.AddProfiling();

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