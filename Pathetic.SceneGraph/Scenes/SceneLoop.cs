using System.Diagnostics;
using Pathetic.Input;
using Pathetic.Renderer;
using Pathetic.SceneGraph.Components;

namespace Pathetic.SceneGraph.Scenes;

public sealed class SceneLoop(
    SceneManager sceneService,
    RenderWindow window,
    ImGuiHost gui,
    RenderGraphHost rdg,
    InputSystem input)
{
    private long _lastTimestamp;
    private float _deltaTime;
    
    public void Run(Scene scene)
    {
        window.Initialize();
        input.Initialize();
        gui.Initialize();
        rdg.Initialize();

        _lastTimestamp = Stopwatch.GetTimestamp();
        
        while (window.Exists)
        {
            var snapshot = window.PumpEvents();
            input.Update(snapshot);
            
            if (!window.Exists)
                break;
            
            window.BeginFrame();
            {
                UpdateDeltaTime();
                scene.FlushSetup();
                UpdateComponents(scene);
                LateUpdateComponents(scene);
            
                rdg.Render(_deltaTime);
        
                gui.Begin(_deltaTime, snapshot);
                GUIComponents(scene);
                gui.End();
                
                scene.FlushDestroyed();
            }
            window.EndFrame();
        }
        
        DestroyComponents(scene);
    }

    private void UpdateDeltaTime()
    {
        _deltaTime = (float)Stopwatch.GetElapsedTime(_lastTimestamp).TotalSeconds;
        _lastTimestamp = Stopwatch.GetTimestamp();
    }
    
    private void UpdateComponents(Scene scene)
    {
        foreach (var obj in scene.Objects)
        {
            foreach (Component component in obj.Components)
            {
                if (!component.IsFullySetup)
                    continue;
                
                ((IComponentLifecycle)component).Update(_deltaTime);
            }
        }   
    }
    
    private void LateUpdateComponents(Scene scene)
    {
        foreach (var obj in scene.Objects)
        {
            foreach (Component component in obj.Components)
            {
                if (!component.IsFullySetup)
                    continue;
                
                ((IComponentLifecycle)component).LateUpdate(_deltaTime);
            }
        }   
    }

    private void GUIComponents(Scene scene)
    {
        foreach (var obj in scene.Objects)
        {
            foreach (Component component in obj.Components)
            {
                if (!component.IsFullySetup)
                    continue;
                
                ((IComponentLifecycle)component).GUI();
            }
        }   
    }

    private void DestroyComponents(Scene scene)
    {
        foreach (var obj in scene.Objects)
        {
            sceneService.DestroyObject(scene, obj);
        }   
    }
}
