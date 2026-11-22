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
using SlangGen = Pathetic.Generated.Slang;
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
    .Present(SlangGen.TemporalAccumulation.Resources.color));

builder.Services.AddStructuredBuffer<SpherePrimitive>(SlangGen.PathTracer.Resources.spheres, sparseStorage: true);
builder.Services.AddStructuredBuffer<PlanePrimitive>(SlangGen.PathTracer.Resources.planes, sparseStorage: true);
builder.Services.AddStructuredBuffer<TrianglePrimitive>(SlangGen.PathTracer.Resources.triangles, sparseStorage: true);
builder.Services.AddStructuredBuffer<MeshTransform>(SlangGen.Primitives.Resources.meshTransforms);
builder.Services.AddStructuredBuffer<Material>(SlangGen.PathTracer.Resources.materials);
builder.Services.AddSingleton<MaterialLibrary>();
builder.Services.AddSingleton<MeshTransformLibrary>();
builder.Services.AddSingleton<ObjLoader>();
builder.Services.AddStructuredBuffer<CameraProvider>();

builder.Services.AddSingleton<SceneBvh>();
builder.Services.AddSingleton(sp => new BvhBufferUploader<GpuBvhNode>(
    SlangGen.PathTracer.Resources.bvhNodes,
    sp.GetRequiredService<SceneBvh>(),
    scene => scene.Nodes));
builder.Services.AddSingleton<IResourceUploader>(sp => sp.GetRequiredService<BvhBufferUploader<GpuBvhNode>>());
builder.Services.AddSingleton(sp => new BvhBufferUploader<GpuBvhPrimitiveRef>(
    SlangGen.PathTracer.Resources.bvhPrimitiveRefs,
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