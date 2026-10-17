namespace Pathetic.SceneGraph;

public class SceneObject : IDestroyable
{
    public required string Name { get; init; }
    public required Scene Owner { get; init; }
    public List<Component> Components { get; } = [];
    public Transform Transform { get; internal set; } = null!;
    public bool IsPendingDestroy { get; internal set; }
    public bool IsDestroyed { get; private set; }

    public T AddComponent<T>() where T : Component
    {
        if (IsPendingDestroy)
            throw new InvalidOperationException("Cannot add a component to a pending-destroy object.");
        
        return Owner.ComponentFactory.CreateComponent<T>(this);
    }

    public void RemoveComponent<T>() where T : Component
    {
        if (IsPendingDestroy)
            throw new InvalidOperationException("Cannot remove a component from a pending-destroy object.");

        var component = GetComponent<T>();
        if (component.IsPendingDestroy)
            return;

        component.IsPendingDestroy = true;
        Owner.PendingDestroy.Add(component);
    }

    public T GetComponent<T>() where T : Component
    {
        var components = GetComponents<T>();
        
        if (components.Length == 0)
            throw new InvalidOperationException($"No component of type {typeof(T)} found on '{Name}'.");
        
        if (components.Length > 1)
            throw new InvalidOperationException($"Multiple components of type {typeof(T).Name} found on '{Name}'.");

        return components[0];
    }

    public bool TryGetComponent<T>(out T? component) where T : Component
    {
        component = Components.OfType<T>().SingleOrDefault();
        return component != null;
    }

    public T[] GetComponents<T>() where T : Component
    {
        return Components.OfType<T>().ToArray();
    }

    public void Destroy()
    {
        if (IsDestroyed)
            return;

        IsDestroyed = true;

        for (int i = Components.Count - 1; i >= 0; i--)
            Components[i].Destroy();

        Components.Clear();
        Owner.Objects.Remove(this);
    }
}
