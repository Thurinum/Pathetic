using Microsoft.Extensions.DependencyInjection;
using Pathetic.Renderer.Primitives;

namespace Pathetic.Renderer.DI;

public sealed class PrimitiveBuilder(IServiceCollection services)
{
    public void Register<T>(string name) where T : unmanaged
    {
        var buffer = new PrimitiveBuffer<T>(name);
        services.AddSingleton(buffer);
        services.AddSingleton<IPrimitiveBuffer>(buffer);
    }
}
