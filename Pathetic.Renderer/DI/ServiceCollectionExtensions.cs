using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pathetic.Renderer.Buffers;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;
using Pathetic.Renderer.Shaders;

namespace Pathetic.Renderer.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public void AddResource<TProvider>() where TProvider : class, IResourceUploader
        {
            services.AddSingleton<TProvider>();
            services.AddSingleton<IResourceUploader>(sp => sp.GetRequiredService<TProvider>());
        }

        public void AddResource<T>(string name, bool sparseStorage = false) where T : unmanaged, IEquatable<T>
        {
            var buffer = new StructuredBuffer<T>(name);
            services.AddSingleton(buffer);
            services.AddSingleton<IResourceUploader>(buffer);

            if (sparseStorage)
                services.AddSingleton(new SparseSet<T>(buffer));
        }
        
        public IServiceCollection AddRenderer(Action<RenderGraphSpecBuilder> configureGraph)
        {
            services.AddSingleton<SlangCompiler>();
            services.AddSingleton<RenderPassFactory>();
            services.AddSingleton<RenderGraphPipelineFactory>();
            services.AddSingleton<SceneRevision>();
            services.AddSingleton<RenderGraphFactory>();
            services.AddSingleton<RenderWindow>();
            services.AddSingleton<ImGuiHost>();
            services.AddSingleton<RenderGraphHost>();
            
            var specBuilder = new RenderGraphSpecBuilder();
            configureGraph(specBuilder);
            services.AddSingleton(Options.Create(specBuilder.Build()));
            
            return services;
        }
    }
}
