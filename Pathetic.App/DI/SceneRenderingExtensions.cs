using Microsoft.Extensions.DependencyInjection;
using Pathetic.App.Assets;
using Pathetic.App.Bvh;
using Pathetic.App.Components;
using Pathetic.App.Components.Camera;
using Pathetic.App.MeshTransforms;
using Pathetic.Renderer.Bvh;
using Pathetic.Renderer.DI;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.DI;

public static class SceneRenderingExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers the scene buffers, their libraries, the BVH and its GPU uploaders.</summary>
        public IServiceCollection AddSceneRendering()
        {
            services.AddStructuredBuffer<Gpu.Sphere>(Gpu.Bindings.PathTracer.Spheres, sparseStorage: true);
            services.AddStructuredBuffer<Gpu.Plane>(Gpu.Bindings.PathTracer.Planes, sparseStorage: true);
            services.AddStructuredBuffer<Gpu.Triangle>(Gpu.Bindings.PathTracer.Triangles, sparseStorage: true);
            services.AddStructuredBuffer<Gpu.MeshTransform>(Gpu.Bindings.Primitives.MeshTransforms);
            services.AddStructuredBuffer<Material>(Gpu.Bindings.PathTracer.Materials);
            services.AddSingleton<MaterialLibrary>();
            services.AddSingleton<MeshTransformLibrary>();
            services.AddSingleton<ObjLoader>();
            services.AddStructuredBuffer<CameraProvider>();

            services.AddSingleton<SceneBvh>();
            services.AddBvhUploader<Gpu.BvhNode>(Gpu.Bindings.PathTracer.BvhNodes, scene => scene.Nodes);
            services.AddBvhUploader<Gpu.BvhPrimitiveRef>(Gpu.Bindings.PathTracer.BvhPrimitiveRefs, scene => scene.PrimitiveRefs);

            return services;
        }

        private IServiceCollection AddBvhUploader<T>(string resourceName, Func<SceneBvh, T[]> selector)
            where T : unmanaged
        {
            services.AddSingleton(sp => new BvhBufferUploader<T>(resourceName, sp.GetRequiredService<SceneBvh>(), selector));
            return services.AddSingleton<IResourceUploader>(sp => sp.GetRequiredService<BvhBufferUploader<T>>());
        }
    }
}
