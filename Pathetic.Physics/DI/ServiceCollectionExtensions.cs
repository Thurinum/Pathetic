using Microsoft.Extensions.DependencyInjection;

namespace Pathetic.Physics.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddPhysics()
        {
            services.AddSingleton<PhysicsRegistry>();
            return services;
        }
    }
}
