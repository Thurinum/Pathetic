using System.Diagnostics;
using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Utils;

namespace Pathetic.Renderer.Resources;

internal readonly record struct ResourceKey(string Name, RenderPass? Owner);

internal sealed class ResourceTable : IResourceAccessor, IDisposable
{
    private readonly Dictionary<ResourceKey, RenderGraphResource> _resources = [];
    private const int DynamicBufferDefaultElementCount = 256;
    private readonly HashSet<string> _reallocated = [];
    private readonly HashSet<string> _resized = [];
    private uint _lastWidth;
    private uint _lastHeight;
    private bool _disposed;

    public void Declare(IResourceSpec spec, RenderPass? owner = null, IResourceUploader? writer = null)
    {
        var resource = new RenderGraphResource
        {
            Spec = spec,
            Writer = writer
        };

        if (!_resources.TryAdd(new ResourceKey(spec.Name, owner), resource))
        {
            throw new ArgumentException($"Resource {spec.Name} already declared.");
        }
    }

    public void Allocate(GraphicsDevice device, uint width, uint height)
    {
        foreach (var entry in _resources.Values)
        {
            entry.Resource = entry.Spec switch
            {
                UniformBufferSpec s => CreateUniformBuffer(device, s),
                TextureSpec s => CreateTexture(device, s, width, height),
                SamplerSpec s => CreateSampler(device, s),
                StructuredBufferSpec s => CreateStructuredBuffer(device, s),
                IDynamicBufferSpec s => CreateDynamicBuffer(device, s, entry, Math.Max(DynamicBufferDefaultElementCount, s.ElementCount)),
                _ => throw new UnreachableException($"Unknown resource spec {entry.Spec.GetType().Name}")
            };
        }

        _lastWidth = width;
        _lastHeight = height;
    }
    
    public IReadOnlySet<string> Upload(GraphicsDevice device, RenderContext ctx)
    {
        _reallocated.Clear();
        
        foreach (RenderGraphResource entry in _resources.Values)
        {
            if (entry.Writer is null)
                continue;

            bool didReallocate = entry.Spec is IDynamicBufferSpec dynamic && Reallocate(device, entry, dynamic);
            entry.Writer.Upload(ctx, (DeviceBuffer)entry.Resource!, didReallocate);
        }

        return _reallocated;
    }

    private bool Reallocate(GraphicsDevice device, RenderGraphResource entry, IDynamicBufferSpec spec)
    {
        uint requiredCapacity = spec.ElementCount;
        if (requiredCapacity <= entry.ElementCapacity)
            return false;

        if (_reallocated.Count == 0)
        {
            device.WaitForIdle();
        }

        _reallocated.Add(entry.Spec.Name);
        uint newCapacity = Math.Max(requiredCapacity, entry.ElementCapacity * 2);
        entry.Resource = CreateDynamicBuffer(device, spec, entry, newCapacity);
        return true;
    }
    
    public IReadOnlySet<string> Resize(GraphicsDevice device, uint width, uint height)
    {
        _resized.Clear();
        
        if (width == _lastWidth && height == _lastHeight)
            return _resized;

        foreach (RenderGraphResource entry in _resources.Values)
        {
            if (entry.Spec is TextureSpec { SizePolicy: AutoSize } spec)
            {
                if (_resized.Count == 0)
                {
                    device.WaitForIdle();
                }
                
                entry.Resource = CreateTexture(device, spec, width, height);
                _resized.Add(entry.Spec.Name);
            }
        }

        _lastWidth = width;
        _lastHeight = height;
        return _resized;
    }
    
    public BindableResource GetResource(string name, RenderPass? owner = null)
    {
        if (owner is not null && _resources.TryGetValue(new ResourceKey(name, owner), out RenderGraphResource? owned))
            return owned.Resource ?? throw new InvalidOperationException($"Accessed resource at {name} but it is null.");

        if (!_resources.TryGetValue(new ResourceKey(name, null), out RenderGraphResource? value))
            throw new ArgumentException($"No resource named {name} registered.");
        
        return value.Resource ?? throw new InvalidOperationException($"Accessed resource at {name} but it is null.");
    }

    public T GetResource<T>(string name, RenderPass? owner = null) where T : class, BindableResource
    {
        return GetResource(name, owner) as T ?? throw new InvalidOperationException($"{name} is not a {typeof(T)}");
    } 
    
    public bool Has(string name, RenderPass? owner = null)
    {
        return Has<BindableResource>(name, owner);
    }
    
    public bool Has<T>(string name, RenderPass? owner = null) where T : BindableResource
    {
        if (owner is not null && _resources.TryGetValue(new ResourceKey(name, owner), out var owned))
            return owned.Resource is T;

        return _resources.TryGetValue(new ResourceKey(name, null), out var shared) && shared.Resource is T;
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        foreach (var resource in _resources.Values)
        {
            (resource.Resource as IDisposable)?.Dispose();
        }

        _resources.Clear();
    }

    private static DeviceBuffer CreateUniformBuffer(GraphicsDevice device, UniformBufferSpec spec)
    {
        var bufferDesc = new BufferDescription(spec.SizeInBytes, BufferUsage.UniformBuffer);
        return device.ResourceFactory.CreateBuffer(bufferDesc);
    }
    
    private static Texture CreateTexture(GraphicsDevice device, TextureSpec spec, uint framebufferWidth, uint framebufferHeight)
    {
        UIntVector2 size = spec.SizePolicy switch
        {
            ExplicitSize s => (s.Width, s.Height),
            AutoSize       => (framebufferWidth, framebufferHeight),
            _ => throw new NotSupportedException()
        };

        return device.ResourceFactory.CreateTexture(new TextureDescription
        {
            Width = size.X, 
            Height = size.Y, 
            Depth = 1,
            MipLevels = 1, 
            ArrayLayers = 1,
            Format = spec.Format, 
            Usage = spec.Usage,
            Type = TextureType.Texture2D, 
            SampleCount = TextureSampleCount.Count1
        });
    }
    
    private static Sampler CreateSampler(GraphicsDevice device, SamplerSpec samplerSpec)
    {
        return device.ResourceFactory.CreateSampler(samplerSpec.Description);
    }
    
    private static DeviceBuffer CreateStructuredBuffer(GraphicsDevice device, StructuredBufferSpec spec)
    {
        var bufferDesc = new BufferDescription(spec.Stride * spec.ElementCount, BufferUsage.StructuredBufferReadOnly, spec.Stride);
        return device.ResourceFactory.CreateBuffer(bufferDesc);
    }
    
    private static DeviceBuffer CreateDynamicBuffer(GraphicsDevice device, IDynamicBufferSpec spec, RenderGraphResource entry, uint elementCapacity = 256)
    {
        ArgumentOutOfRangeException.ThrowIfZero(elementCapacity);
        
        entry.ElementCapacity = elementCapacity;
        var bufferDesc = new BufferDescription(spec.Stride * entry.ElementCapacity, BufferUsage.StructuredBufferReadOnly | BufferUsage.Dynamic, spec.Stride);
        return device.ResourceFactory.CreateBuffer(bufferDesc);
    }
}