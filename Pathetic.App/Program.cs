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

builder.Services.AddRenderer(passes =>
{
    passes.AddPass<CirclesPass>();
    passes.AddPass<PathTracerPass>();
    passes.AddPass<TemporalAccumulationPass>();
    passes.Present(Resources.Color);
}, primitives =>
{
    primitives.Register<SpherePrimitive>(Resources.Spheres);
    primitives.Register<PlanePrimitive>(Resources.Planes);
    primitives.Register<CirclePrimitive>(Resources.CirclePrimitives);
});

builder.Services.AddRenderResourceProvider<CameraProvider>();
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