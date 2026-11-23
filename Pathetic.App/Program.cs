using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pathetic.App;
using Pathetic.App.Components.Camera;
using Pathetic.App.DI;
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
    .Present(Gpu.Bindings.TemporalAccumulation.Color));

builder.Services.AddSceneRendering();
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