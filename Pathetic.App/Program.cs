using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pathetic.App;
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

builder.Services.AddSingleton<ConvergenceController>();
builder.Services.AddSingleton<RaycastService>();

builder.Services.AddRenderer(graph => graph
    // .AddPass<CirclesPass>()
    .AddPass<PathTracerPass>()
    .AddPass<TemporalAccumulationPass>()
    .AddPass<PrimaryRayPass>()
    .Present(ResourceName.PresentColor));

builder.Services.AddStructuredBuffer<SpherePrimitive>(ResourceName.Spheres, sparseStorage: true);
builder.Services.AddStructuredBuffer<PlanePrimitive>(ResourceName.Planes, sparseStorage: true);
builder.Services.AddWritableBuffer<PrimaryHit>(ResourceName.PrimaryHits);
builder.Services.AddStructuredBuffer<CameraProvider>();
// builder.Services.AddStructuredBuffer<CirclePrimitive>(ResourceName.Circles, sparseStorage: true);

builder.Services.AddSingleton<SceneBvh>();
builder.Services.AddSingleton(sp => new BvhBufferUploader<GpuBvhNode>(
    ResourceName.BvhNodes,
    sp.GetRequiredService<SceneBvh>(),
    scene => scene.Nodes));
builder.Services.AddSingleton<IResourceUploader>(sp => sp.GetRequiredService<BvhBufferUploader<GpuBvhNode>>());
builder.Services.AddSingleton(sp => new BvhBufferUploader<GpuBvhPrimitiveRef>(
    ResourceName.BvhPrimitiveRefs,
    sp.GetRequiredService<SceneBvh>(),
    scene => scene.PrimitiveRefs));
builder.Services.AddSingleton<IResourceUploader>(sp => sp.GetRequiredService<BvhBufferUploader<GpuBvhPrimitiveRef>>());

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