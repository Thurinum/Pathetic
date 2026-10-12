using System.Diagnostics;
using Pathetic.Renderer;

namespace Pathetic.SceneGraph;

public class SceneLoop(RenderBackend backend)
{
    private long _lastTimestamp;
    private float _deltaTime;
    
    public void Run(Scene scene)
    {
        _lastTimestamp = Stopwatch.GetTimestamp();
        
        AwakeComponents(scene);
        StartComponents(scene);
        
        while (backend.Shown)
        {
            var input = backend.GetInput(); // todo how to pass input to components
            
            if (!backend.Shown)
                break;
            
            backend.BeginFrame();
            {
                UpdateDeltaTime();
                UpdateComponents(scene);
                LateUpdateComponents(scene);
            
                backend.Render(_deltaTime);
        
                backend.BeginGUI(_deltaTime, input);
                GUIComponents(scene);
                backend.EndGUI();
                
                scene.FlushDestroyed();
            }
            backend.EndFrame();
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
