using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PathTracerApp;
using PathTracerApp.RenderPasses;
using PathTracerCore;

using var provider = ConfigureServices();
var app = provider.GetRequiredService<App>();
app.Run();
return;

ServiceProvider ConfigureServices()
{
    ServiceCollection services = new();

    services.AddLogging(builder => builder.AddConsole());
    services.AddEngineCore(options =>
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
    });
    services.AddSingleton<App>();
        
    return services.BuildServiceProvider();
}
