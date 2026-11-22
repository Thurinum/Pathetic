using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Pathetic.Profiling.Gpu;
using Pathetic.Profiling.Statistics;

namespace Pathetic.Profiling.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddProfiling()
        {
            services.AddOptions<ProfilerOptions>()
                .BindConfiguration(ProfilerOptions.Section)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton<ProfilerStats>();
            services.AddSingleton<ShaderCounterStats>();
            services.AddHostedService<ProfilerHost>();
            services.AddSingleton<FenceGpuTimer>();

            services.Replace(ServiceDescriptor.Singleton<IGpuTimer>(provider =>
                provider.GetRequiredService<IOptions<ProfilerOptions>>().Value.Enabled
                    ? provider.GetRequiredService<FenceGpuTimer>()
                    : new NullGpuTimer()));

            return services;
        }
    }
}
