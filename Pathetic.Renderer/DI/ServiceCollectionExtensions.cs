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
        public void AddRenderResourceProvider<T>() where T : class, IResourceProvider
        {
            services.AddSingleton<T>();
            services.AddSingleton<IResourceProvider>(sp => sp.GetRequiredService<T>());
        }
        
        public void AddPrimitive<T>(string name) where T : unmanaged, IEquatable<T>
        {
            var buffer = new DirtyTrackingBuffer<T>(name);
            services.AddSingleton(buffer);
            services.AddSingleton(new SparseSet<T>(buffer));
            services.AddSingleton<IDynamicBufferSpec>(buffer);
        }
        
        public IServiceCollection AddRenderer(Action<RenderGraphSpecBuilder> configureGraph)
        {
            services.AddSingleton<PrimitiveResourceProvider>();
            services.AddSingleton<IResourceProvider>(sp => sp.GetRequiredService<PrimitiveResourceProvider>());
            
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
