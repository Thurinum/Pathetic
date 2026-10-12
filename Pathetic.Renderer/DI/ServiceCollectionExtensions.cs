using Microsoft.Extensions.DependencyInjection;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Primitives;
using Pathetic.Renderer.Resources;
using Pathetic.Renderer.Shaders;

namespace Pathetic.Renderer.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public void AddRenderResourceProvider<T>() where T : class, IResourceProvider
        {
            services.AddSingleton<T>();
            services.AddSingleton<IResourceProvider>(sp => sp.GetRequiredService<T>());
        }
        
        public IServiceCollection AddRenderer(
            Action<RenderGraphBuilder> configurePasses,
            Action<PrimitiveBuilder>? configurePrimitives = null)
        {
            var passSelector = new RenderGraphBuilder();
            configurePasses(passSelector);
            services.AddSingleton(passSelector);
            
            if (passSelector.PassTypes.Count == 0)
                throw new InvalidOperationException("No render pass selected");

            var primitives = new PrimitiveBuilder(services);
            configurePrimitives?.Invoke(primitives);
            services.AddSingleton<PrimitiveResourceProvider>();
            services.AddSingleton<IResourceProvider>(sp => sp.GetRequiredService<PrimitiveResourceProvider>());
            
            services.AddSingleton<SlangCompiler>();
            services.AddSingleton<RenderPassFactory>();
            services.AddSingleton<RenderGraphPipelineFactory>();
            services.AddSingleton<RenderGraph>();
            services.AddSingleton<RenderBackend>();
            services.AddSingleton<SceneRevision>();
            
            return services;
        }
    }
}
