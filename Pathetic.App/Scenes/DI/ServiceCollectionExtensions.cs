using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Pathetic.App.Scenes.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddScene<TScene>() where TScene : class, ISceneDefinition
        {
            services.TryAddSingleton<SceneFactory>();
            return services.AddSingleton<ISceneDefinition, TScene>();
        }
    }
}
