using NeoVeldrid;
using Pathetic.Renderer.Resources;

namespace Pathetic.Renderer.Graph;

internal static class RenderGraphValidator
{
    public static string ValidateSpec(RenderGraphSpec spec)
    {
        if (spec.PassTypes.Count == 0)
            throw new InvalidOperationException("No render pass selected");

        return spec.PresentTexture
            ?? throw new InvalidOperationException("No present texture is set.");
    }

    public static void ValidatePresentTexture(string presentTexture, ResourceTable resources, Framebuffer framebuffer)
    {
        if (!resources.Has<Texture>(presentTexture))
            throw new InvalidOperationException($"Present texture {presentTexture} isn't a valid resource.");

        if (resources.GetResource<Texture>(presentTexture).Format != framebuffer.ColorTargets[0].Target.Format)
            throw new InvalidOperationException($"Present tex {presentTexture} must have same format as the swapchain.");
    }

    public static void ValidatePassBindings(List<PassEntry> passes, ResourceTable resources)
    {
        foreach (var entry in passes)
        {
            string passName = entry.Pass.GetType().Name;
            string shader = entry.Pass.ShaderModule;

            var bindings = new Dictionary<string, ResourceLayoutElementDescription>(StringComparer.Ordinal);
            foreach (var binding in entry.State!.Bindings)
                bindings.TryAdd(binding.Name, binding);

            var declared = entry.Pass.Inputs
                .Concat(entry.Pass.Outputs.Select(o => o.Name))
                .Concat(entry.Pass.Samplers.Select(s => s.Name))
                .ToHashSet(StringComparer.Ordinal);

            foreach (var input in entry.Pass.Inputs)
            {
                if (!resources.Has(input))
                    throw new InvalidOperationException($"Pass {passName} has invalid input {input}.");

                if (!bindings.ContainsKey(input))
                    throw new InvalidOperationException($"Pass {passName} declares input '{input}' that shader '{shader}' does not consume.");
            }

            foreach (var sampler in entry.Pass.Samplers)
            {
                if (!bindings.TryGetValue(sampler.Name, out var binding))
                    throw new InvalidOperationException($"Pass {passName} declares sampler '{sampler.Name}' that shader '{shader}' does not consume.");

                if (binding.Kind != ResourceKind.Sampler)
                    throw new InvalidOperationException($"Pass {passName} sampler '{sampler.Name}': shader '{shader}' declares {binding.Kind}, expected {ResourceKind.Sampler}.");
            }

            foreach (var binding in bindings.Values)
            {
                if (!declared.Contains(binding.Name))
                    throw new InvalidOperationException($"Pass {passName} shader '{shader}' binds '{binding.Name}', which the pass does not declare as an Input, Output, or Sampler.");
            }
        }
    }
}
