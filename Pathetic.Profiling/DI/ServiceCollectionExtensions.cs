using Microsoft.Extensions.DependencyInjection;
#if PROFILING
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pathetic.Profiling.Gpu;
using Pathetic.Profiling.Statistics;
#endif

namespace Pathetic.Profiling.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddProfiling()
        {
#if PROFILING
            services.AddOptions<ProfilerOptions>()
                .BindConfiguration(ProfilerOptions.Section)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton<ProfilerStats>();
            services.AddSingleton<ShaderCounterStats>();
            services.AddHostedService<ProfilerHost>();
            services.AddSingleton<FenceGpuTimer>();

            services.Replace(ServiceDescriptor.Singleton<IGpuTimer>(provider => provider.GetRequiredService<FenceGpuTimer>()));
#endif

            return services;
        }
    }
}
