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
        public void AddStructuredBuffer<TProvider>() where TProvider : class, IResourceUploader
        {
            services.AddSingleton<TProvider>();
            services.AddSingleton<IResourceUploader>(sp => sp.GetRequiredService<TProvider>());
        }

        /// <summary>
        /// Registers a type of buffer for upload from the CPU to the GPU.
        /// </summary>
        /// <param name="name">Name of the resource as used in shaders.</param>
        /// <param name="sparseStorage">Whether to use a sparse set, which provides safe element removal while preserving buffer packing.</param>
        /// <param name="resizable">Whether to auto-grow the buffer when capacity is reached.</param>
        /// <param name="dynamic">Whether the buffer will be updated frequently.</param>
        /// <param name="capacity">How many elements the buffer can hold.</param>
        /// <typeparam name="T"></typeparam>
        public void AddStructuredBuffer<T>(
            string name,
            bool sparseStorage = false,
            bool resizable = true,
            bool dynamic = true,
            uint capacity = 256) where T : unmanaged, IEquatable<T>
        {
            var buffer = new StructuredBuffer<T>(name, resizable, dynamic, capacity, 64);
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
