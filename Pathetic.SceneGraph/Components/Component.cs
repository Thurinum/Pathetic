using Microsoft.Extensions.Logging;
using Pathetic.SceneGraph.Scene;

namespace Pathetic.SceneGraph.Components;

public abstract class Component : IDestroyable, IComponentLifecycle
{
    public SceneObject Parent { get; internal set; } = null!;
    public ILogger Logger { get; internal set; } = null!;
    protected Scene.Scene Root => Parent.Owner;
    
    public bool IsFullySetup { get; private set; }
    public bool IsPendingDestroy { get; internal set; }
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

    public void Destroy()
    {
        if (IsDestroyed)
            return;

        Cleanup();
        Parent.Components.Remove(this);
        IsDestroyed = true;
    }
}