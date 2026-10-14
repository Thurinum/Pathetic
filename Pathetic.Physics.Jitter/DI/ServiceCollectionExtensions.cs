using Microsoft.Extensions.DependencyInjection;

namespace Pathetic.Physics.Jitter.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddJitterPhysics()
        {
            services.AddSingleton<IPhysicsWorld, JitterPhysicsWorld>();
            return services;
        }
    }
}
