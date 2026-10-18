using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pathetic.App;
using Pathetic.App.Components;
using Pathetic.App.Components.Camera;
using Pathetic.App.RenderPasses;
using Pathetic.Renderer;
using Pathetic.Renderer.DI;
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

builder.Services.AddRenderer(graph => graph
    .AddPass<CirclesPass>()
    .AddPass<PathTracerPass>()
    .AddPass<TemporalAccumulationPass>()
    .Present(ResourceName.Color));

builder.Services.AddStructuredBuffer<SpherePrimitive>(ResourceName.Spheres, sparseStorage: true);
builder.Services.AddStructuredBuffer<PlanePrimitive>(ResourceName.Planes, sparseStorage: true);
builder.Services.AddStructuredBuffer<CirclePrimitive>(ResourceName.CirclePrimitives, sparseStorage: true);

builder.Services.AddStructuredBuffer<CameraProvider>();
builder.Services.AddSceneGraph();
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