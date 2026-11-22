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
        /// <typeparam name="TSource">Describes the counters the shaders write.</typeparam>
        public IServiceCollection AddProfiling<TSource>() where TSource : class, IShaderCounterSource
        {
            services.AddOptions<ProfilerOptions>()
                .BindConfiguration(ProfilerOptions.Section)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.Replace(ServiceDescriptor.Singleton<IShaderCounterSource, TSource>());
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
