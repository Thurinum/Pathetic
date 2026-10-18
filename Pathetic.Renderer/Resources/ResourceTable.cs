using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Utils;

namespace Pathetic.Renderer.Resources;

internal readonly record struct ResourceKey(string Name, RenderPass? Owner);

internal sealed class ResourceTable : IResourceMetadata, IDisposable
{
    private const string FrameStateName = "frameState";
    private readonly Dictionary<ResourceKey, RenderGraphResource> _resources = [];
    private readonly HashSet<string> _reallocated = [];
    private readonly HashSet<string> _resized = [];
    private uint _lastWidth;
    private uint _lastHeight;
    private bool _disposed;
    private IReadOnlySet<string>? _visibleResources;

    public ResourceTable()
    {
        Declare(UniformBufferSpec.Of<FrameState>(FrameStateName));
    }

    private SceneChange _pendingChanges;
    public SceneChange SceneRevision { get; private set; }

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
                IStructuredBufferSpec s => CreateBuffer(device, s, entry, Math.Max(s.InitialCapacity, s.ElementCount)),
                _ => throw new UnreachableException($"Unknown resource spec {entry.Spec.GetType().Name}")
            };
        }

        _lastWidth = width;
        _lastHeight = height;
    }
    
    public IReadOnlySet<string> Upload(GraphicsDevice device, RenderContext ctx)
    {
        _reallocated.Clear();
        SceneRevision = _pendingChanges;
        _pendingChanges = SceneChange.None;

        foreach (RenderGraphResource entry in _resources.Values)
        {
            // Frame data has no writer; pass parameters are derived from the scene.
            if (entry.Writer is null or RenderPass)
                continue;

            bool didReallocate = entry.Spec is IStructuredBufferSpec spec && Reallocate(device, entry, spec);
            bool visible = IsVisible(entry.Spec.Name);

            if (entry.Writer.DirtyChanges == SceneChange.None && !didReallocate)
                continue;

            if (visible)
                SceneRevision |= entry.Writer.DirtyChanges;

            // ignore textures and samplers
            // TODO: Texture upload path from CPU
            if (entry.Resource is not DeviceBuffer buffer)
                continue;

            entry.Writer.Upload(ctx, buffer, didReallocate);
            entry.Writer.ClearDirty();

            if (didReallocate && visible)
                SceneRevision |= SceneChange.Allocation;
        }

        foreach (RenderGraphResource entry in _resources.Values)
        {
            if (entry.Writer is not RenderPass pass || entry.Resource is not DeviceBuffer buffer)
                continue;

            pass.Upload(ctx, buffer, false);
        }

        // Hard-coded and deliberately last: shaders must observe this frame's revision.
        ctx.Cmd.UpdateBuffer(GetResource<DeviceBuffer>(FrameStateName), 0, new FrameState
        {
            FrameIndex = ctx.FrameIndex,
            DeltaTime = ctx.DeltaTime,
            SceneRevision = SceneRevision
        });

        return _reallocated;
    }

    public void SetVisibleResources(IReadOnlySet<string> names) => _visibleResources = names;

    private bool IsVisible(string name) => _visibleResources?.Contains(name) ?? true;

    private bool Reallocate(GraphicsDevice device, RenderGraphResource entry, IStructuredBufferSpec spec)
    {
        uint requiredCapacity = spec.ElementCount;
        if (requiredCapacity <= entry.ElementCapacity)
            return false;

        if (!spec.IsResizable)
        {
            throw new InvalidOperationException($"{spec.Name} overflowed its fixed capacity: {requiredCapacity} > {entry.ElementCapacity}.");
        }

        if (_reallocated.Count == 0)
        {
            device.WaitForIdle();
        }

        _reallocated.Add(entry.Spec.Name);
        uint newCapacity = Math.Max(requiredCapacity, entry.ElementCapacity * 2);
        entry.Resource = CreateBuffer(device, spec, entry, newCapacity);
        return true;
    }
    
    public IReadOnlySet<string> Resize(GraphicsDevice device, uint width, uint height)
    {
        _resized.Clear();
        
        if (width == _lastWidth && height == _lastHeight)
            return _resized;

        foreach (RenderGraphResource entry in _resources.Values)
        {
            if (entry.Spec is not TextureSpec { SizePolicy: AutoSize } spec) 
                continue;
            
            if (_resized.Count == 0)
            {
                device.WaitForIdle();
            }
                
            entry.Resource = CreateTexture(device, spec, width, height);
            _resized.Add(entry.Spec.Name);
        }

        if (_resized.Count > 0)
            _pendingChanges |= SceneChange.Allocation;

        _lastWidth = width;
        _lastHeight = height;
        return _resized;
    }
    
    public BindableResource GetResource(string name, RenderPass? owner = null)
    {
        if (!TryFind(name, owner, out RenderGraphResource? entry))
            throw new ArgumentException($"No resource named {name} registered.");

        return entry.Resource ?? throw new InvalidOperationException($"Accessed resource at {name} but it is null.");
    }

    public T GetResource<T>(string name, RenderPass? owner = null) where T : class, BindableResource
    {
        return GetResource(name, owner) as T ?? throw new InvalidOperationException($"{name} is not a {typeof(T)}");
    }

    public uint GetElementCount(string name, RenderPass? owner = null)
    {
        if (!TryFind(name, owner, out RenderGraphResource? entry))
            throw new ArgumentException($"No resource named {name} registered.");

        return entry.Spec is IStructuredBufferSpec dynamicSpec
            ? dynamicSpec.ElementCount
            : throw new InvalidOperationException($"{name} is not a dynamic buffer and has no element count.");
    }

    public bool Has(string name, RenderPass? owner = null)
    {
        return Has<BindableResource>(name, owner);
    }

    public bool Has<T>(string name, RenderPass? owner = null) where T : BindableResource
    {
        return TryFind(name, owner, out RenderGraphResource? entry) && entry.Resource is T;
    }

    private bool TryFind(string name, RenderPass? owner, [NotNullWhen(true)] out RenderGraphResource? entry)
    {
        if (owner is not null && _resources.TryGetValue(new ResourceKey(name, owner), out entry))
            return true;

        return _resources.TryGetValue(new ResourceKey(name, null), out entry);
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
    
    private static DeviceBuffer CreateBuffer(GraphicsDevice device, IStructuredBufferSpec spec, RenderGraphResource entry, uint elementCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfZero(elementCapacity);

        entry.ElementCapacity = elementCapacity;
        BufferUsage usage = BufferUsage.StructuredBufferReadOnly;
        if (spec.IsResizable)
            usage |= BufferUsage.Dynamic;

        var bufferDesc = new BufferDescription(spec.Stride * elementCapacity, usage, spec.Stride);
        return device.ResourceFactory.CreateBuffer(bufferDesc);
    }
}