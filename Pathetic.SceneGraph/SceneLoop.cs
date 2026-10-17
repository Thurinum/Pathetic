using System.Diagnostics;
using Pathetic.Renderer;

namespace Pathetic.SceneGraph;

public class SceneLoop(RenderWindow window, ImGuiHost gui, RenderGraphHost rdg)
{
    private long _lastTimestamp;
    private float _deltaTime;
    
    public void Run(Scene scene)
    {
        window.Initialize();
        gui.Initialize();
        rdg.Initialize();

        _lastTimestamp = Stopwatch.GetTimestamp();
        
        AwakeComponents(scene);
        StartComponents(scene);
        
        while (window.Exists)
        {
            var input = window.PumpEvents(); // todo how to pass input to components
            
            if (!window.Exists)
                break;
            
            window.BeginFrame();
            {
                UpdateDeltaTime();
                UpdateComponents(scene);
                LateUpdateComponents(scene);
            
                rdg.Render(_deltaTime);
        
                gui.Begin(_deltaTime, input);
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
    
    private static void AwakeComponents(Scene scene)
    {
        foreach (var obj in scene.Objects)
        {
            foreach (var component in obj.Components)
            {
                component.Awake();
            }
        }   
    }
    
    private static void StartComponents(Scene scene)
    {
        foreach (var obj in scene.Objects)
        {
            foreach (var component in obj.Components)
            {
                component.Start();
            }
        }   
    }
    
    private void UpdateComponents(Scene scene)
    {
        foreach (var obj in scene.Objects)
        {
            foreach (var component in obj.Components)
            {
                component.Update(_deltaTime);
            }
        }   
    }
    
    private void LateUpdateComponents(Scene scene)
    {
        foreach (var obj in scene.Objects)
        {
            foreach (var component in obj.Components)
            {
                component.LateUpdate(_deltaTime);
            }
        }   
    }

    private static void GUIComponents(Scene scene)
    {
        foreach (var obj in scene.Objects)
        {
            foreach (var component in obj.Components)
            {
                component.OnGUI();
            }
        }   
    }

    private static void DestroyComponents(Scene scene)
    {
        foreach (var obj in scene.Objects)
        {
            foreach (var component in obj.Components)
            {
                component.OnDestroy();
            }
        }   
    }
}
