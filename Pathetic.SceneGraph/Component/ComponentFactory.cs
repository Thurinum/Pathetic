using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pathetic.SceneGraph.Scene;

namespace Pathetic.SceneGraph.Component;

public sealed class ComponentFactory(IServiceProvider provider, ILoggerFactory loggerFactory)
{
    public T CreateComponent<T>(SceneObject parent) where T : Component
    {
        T component = ActivatorUtilities.CreateInstance<T>(provider);
        component.Parent = parent;
        component.Logger = loggerFactory.CreateLogger<T>();
        parent.Components.Add(component);
        return component;
    }
}
