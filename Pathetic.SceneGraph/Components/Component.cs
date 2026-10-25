using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Pathetic.SceneGraph.Scenes;

namespace Pathetic.SceneGraph.Components;

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract class Component : IComponentLifecycle, IDestroyable
{
    public SceneObject Parent { get; internal set; } = null!;
    public ILogger Logger { get; internal set; } = null!;
    protected Scene Root => Parent.Owner;
    
    public bool IsFullySetup { get; private set; }
    public bool IsPendingDestroy { get; internal set; }
    bool IDestroyable.IsPendingDestroy { get => IsPendingDestroy; set => IsPendingDestroy = value;}
    public bool IsDestroyed { get; private set; }
    
    void IComponentLifecycle.Setup() => Setup();
    void IComponentLifecycle.LateSetup() { LateSetup(); IsFullySetup = true; }
    void IComponentLifecycle.Update(float deltaTime) { Update(deltaTime);}
    void IComponentLifecycle.LateUpdate(float deltaTime) { LateUpdate(deltaTime);}
    void IComponentLifecycle.GUI() { GUI(); }
    void IComponentLifecycle.Cleanup() { Cleanup(); }
    
    protected virtual void Setup() {}
    protected virtual void LateSetup() {}
    protected virtual void Update(float deltaTime) {}
    protected virtual void LateUpdate(float deltaTime) {}
    protected virtual void GUI() {}
    protected virtual void Cleanup() {}

    void IDestroyable.Destroy()
    {
        if (IsDestroyed)
            return;
        
        IsDestroyed = true;
        
        try
        {
            Cleanup();
        }
        finally
        {
            Parent.Components.Remove(this);
        }
    }
}