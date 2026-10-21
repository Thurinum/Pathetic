using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Pathetic.SceneGraph;

public sealed class ComponentFactory(IServiceProvider provider, ILoggerFactory loggerFactory)
{
    public T CreateComponent<T>(SceneObject parent) where T : Component
    {
        var component = ActivatorUtilities.CreateInstance<T>(provider);
        component.Parent = parent;
        component.Logger = loggerFactory.CreateLogger<T>();
        component.Awake();
        component.Start();
        parent.Components.Add(component);
        return component;
    }
}
