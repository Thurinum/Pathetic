namespace Pathetic.SceneGraph;

public class Scene(ComponentFactory componentFactory)
{
    public List<SceneObject> Objects { get; } = [];
    
    internal ComponentFactory ComponentFactory { get; } = componentFactory;
    internal List<IDestroyable> PendingDestroy { get; } = [];

    private readonly List<Func<SceneObject>> _pendingCreate = [];

    // Defers object creation to the end of the frame so it happens outside the component iteration loops
    // (adding to Objects while SceneLoop is enumerating it would throw).
    public void DeferCreate(Func<SceneObject> factory) => _pendingCreate.Add(factory);

    public void FlushCreated()
    {
        if (_pendingCreate.Count == 0)
            return;

        var batch = _pendingCreate.ToArray();
        _pendingCreate.Clear();

        foreach (var factory in batch)
        {
            var created = factory();

            foreach (var component in created.Components)
                component.Awake();

            foreach (var component in created.Components)
                component.Start();
        }
    }

    public void FlushDestroyed()
    {
        if (PendingDestroy.Count == 0)
            return;

        var batch = PendingDestroy.ToArray();
        PendingDestroy.Clear();

        foreach (var destroyable in batch)
            destroyable.Destroy();
    }
}
