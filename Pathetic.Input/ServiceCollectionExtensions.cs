using Microsoft.Extensions.DependencyInjection;

namespace Pathetic.Input;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInputSystem(InputContext[] inputContexts)
        {
            Type[] devices = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(asm => asm.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(InputDevice)) && !t.IsAbstract)
                .ToArray();

            foreach (Type device in devices)
            {
                services.AddSingleton(typeof(InputDevice), device);
            }

            Console.WriteLine($"Discovered {devices.Length} input devices: {string.Join(", ", devices.Select(t => t.Name))}");

            if (inputContexts.Length == 0)
                throw new ArgumentException("No input contexts given.");

            foreach (InputContext context in inputContexts)
            {
                services.AddSingleton<InputContext>(context);
            }

            services.AddSingleton<InputSystem>();
            return services;
        }
    }
}
