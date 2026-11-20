using Microsoft.Extensions.DependencyInjection;

namespace Pathetic.App.Scenes.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddScene<TScene>() where TScene : class, IScene =>
            services
                .AddSingleton<IScene, TScene>()
                .AddSingleton<SceneFactory>();
    }
}
