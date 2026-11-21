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
        /// <typeparam name="TLayout">The generated description of the shader counters.</typeparam>
        /// <typeparam name="TStats">The class that presents the shader counters.</typeparam>
        public IServiceCollection AddProfiling<TLayout, TStats>()
            where TLayout : class, IShaderCounterLayout
            where TStats : ShaderCounterStats
        {
            services.AddOptions<ProfilerOptions>()
                .BindConfiguration(ProfilerOptions.Section)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.Replace(ServiceDescriptor.Singleton<IShaderCounterLayout, TLayout>());
            services.AddSingleton<ProfilerStats>();
            services.AddSingleton<ShaderCounterStats, TStats>();
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
