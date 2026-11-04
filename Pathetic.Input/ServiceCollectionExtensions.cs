using Microsoft.Extensions.DependencyInjection;

namespace Pathetic.Input;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInputSystem(InputContext[] inputContexts)
        {
            var devices = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(asm => asm.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(InputDevice)));

            foreach (var device in devices)
            {
                services.AddSingleton(typeof(InputDevice), device);
            }

            if (inputContexts.Length == 0)
                throw new ArgumentException("No input contexts given.");

            services.AddSingleton(inputContexts);
            return services;
        }
    }
}
