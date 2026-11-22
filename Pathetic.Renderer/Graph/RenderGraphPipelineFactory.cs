using NeoVeldrid;
using Pathetic.Renderer.Resources;
using Pathetic.Renderer.Shaders;

namespace Pathetic.Renderer.Graph;

public sealed class RenderGraphPipelineFactory(PrecompiledShaderLibrary shaders)
{
    public PassRenderState Build(GraphicsDevice device, RenderPass pass)
    {
        ShaderCompilationResult module = shaders.LoadComputeShader(pass.ShaderModule);
        ShaderDescription shaderDesc = new(ShaderStages.Compute, module.Code, PrecompiledShaderLibrary.EntryPointName);
        using Shader shader = device.ResourceFactory.CreateShader(shaderDesc);

        var bindings = module.Reflection
            .Select(r => new ResourceLayoutElementDescription(r.Name, r.Kind, ShaderStages.Compute))
            .ToArray();

        ResourceLayoutDescription layoutDesc = new(bindings);
        ResourceLayout layout = device.ResourceFactory.CreateResourceLayout(layoutDesc);

        (uint x, uint y, uint z) = module.GroupSize;
        ComputePipelineDescription psoDesc = new(shader, layout, x, y, z);
        Pipeline pso = device.ResourceFactory.CreateComputePipeline(psoDesc);

        var state = new PassRenderState
        {
            ThreadGroupSize = module.GroupSize,
            Layout = layout,
            Bindings = bindings,
            Pipeline = pso
        };
        return state;
    }

    internal static void UpdateResourceSet(PassRenderState state, RenderPass owner, GraphicsDevice device, ResourceTable table)
    {
        state.Set?.Dispose();
        BindableResource[] resources = state.Bindings.Select(b => table.GetResource(b.Name, owner)).ToArray();
        ResourceSetDescription setDesc = new(state.Layout, resources);
        state.Set = device.ResourceFactory.CreateResourceSet(setDesc);
    }
}