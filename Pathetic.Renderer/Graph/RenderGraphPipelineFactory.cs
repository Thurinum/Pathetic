using NeoVeldrid;
using Pathetic.Renderer.Resources;
using Pathetic.Renderer.Shaders;

namespace Pathetic.Renderer.Graph;

public sealed class RenderGraphPipelineFactory(SlangCompiler compiler)
{
    public PassRenderState Build(GraphicsDevice device, RenderPass pass)
    {
        ShaderCompilationResult compiled = compiler.CompileComputeShader(pass.ShaderModule);
        ShaderDescription shaderDesc = new(ShaderStages.Compute, compiled.Code, SlangCompiler.EntryPointName);
        using Shader shader = device.ResourceFactory.CreateShader(shaderDesc);

        var bindings = compiled.Reflection
            .Select(r => new ResourceLayoutElementDescription(r.Name, r.Kind, ShaderStages.Compute))
            .ToArray();

        ResourceLayoutDescription layoutDesc = new(bindings);
        ResourceLayout layout = device.ResourceFactory.CreateResourceLayout(layoutDesc);

        (uint x, uint y, uint z) = compiled.GroupSize;
        ComputePipelineDescription psoDesc = new(shader, layout, x, y, z);
        Pipeline pso = device.ResourceFactory.CreateComputePipeline(psoDesc);

        var state = new PassRenderState
        {
            ThreadGroupSize = compiled.GroupSize,
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