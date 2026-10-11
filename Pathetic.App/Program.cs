using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pathetic.App;
using Pathetic.App.Components;
using Pathetic.App.Components.Camera;
using Pathetic.App.RenderPasses;
using Pathetic.Renderer.DI;
using Pathetic.SceneGraph.DI;

using var provider = ConfigureServices();
var app = provider.GetRequiredService<App>();
app.Run();
return;

ServiceProvider ConfigureServices()
{
    ServiceCollection services = new();

    services.AddLogging(builder => builder.AddConsole());
    services.AddRenderer(options =>
    {
        options.WindowWidth = 1920;
        options.WindowHeight = 1080;
        options.X = 100;
        options.Y = 100;
        options.SamplesPerPixel = 64;
        options.MaxBounces = 8;
    }, passes =>
    {
        passes.AddPass<CirclesPass>();
        passes.AddPass<PathTracerPass>();
        passes.AddPass<TemporalAccumulationPass>();
        passes.Present("color");
    }, primitives =>
    {
        primitives.Register<SpherePrimitive>("spheres");
        primitives.Register<PlanePrimitive>("planes");
        primitives.Register<CirclePrimitive>("circlePrimitives");
    });
    
    services.AddSingleton<TemporalAccumulationSettings>();
    services.AddRenderResourceProvider<CameraProvider>();
    services.AddSceneGraph();
    services.AddSingleton<App>(); 
    
    return services.BuildServiceProvider();
}
