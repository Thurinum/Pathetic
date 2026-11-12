using Microsoft.Extensions.Logging;
using Pathetic.App.Components.Camera;
using Pathetic.App.Input;
using Pathetic.App.RenderPasses;
using Pathetic.Renderer.Utils;
using Pathetic.SceneGraph.Components;

namespace Pathetic.App.Components.GUI;

public class EditorSelection(InputActions input, RaycastService raycast, CameraProvider camera) : Component
{
    protected override void Setup()
    {
        input.EditorViewport.ToggleSelect.Triggered += StartSelect; 
        raycast.ResultReceived += Select;
    }

    private void StartSelect()
    {
        Ray ray = camera.ScreenToCamera(input.EditorViewport.MousePosition.Value.XY);
        raycast.Raycast(ray);
    }

    private void Select(HitResult hit)
    {

        Logger.LogInformation("Hit {Obj}", hit.Index);  
    }
}