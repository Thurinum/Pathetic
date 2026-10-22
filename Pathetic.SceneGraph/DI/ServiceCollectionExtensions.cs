using Microsoft.Extensions.DependencyInjection;

namespace Pathetic.SceneGraph.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddSceneGraph()
        {
            services.AddSingleton<ComponentFactory>();
            services.AddSingleton<SceneManager>();
            services.AddSingleton<SceneLoop>();
            return services;
        }
    }
}
