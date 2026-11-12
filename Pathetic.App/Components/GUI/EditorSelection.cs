using Microsoft.Extensions.Logging;
using Pathetic.App.Components.Camera;
using Pathetic.App.Input;
using Pathetic.App.RenderPasses;
using Pathetic.Renderer.Buffers;
using Pathetic.Renderer.Utils;
using Pathetic.SceneGraph;
using Pathetic.SceneGraph.Components;

namespace Pathetic.App.Components.GUI;

public class EditorSelection(
    InputActions input,
    RaycastService raycast,
    CameraProvider camera,
    PrimitiveRegistry registry,
    SparseSet<SpherePrimitive> spheres,
    SparseSet<PlanePrimitive> planes) : Component
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
        if (!hit.IsHit)
        {
            Logger.LogInformation("Hit {Obj}", "nothing");
            return;
        }

        Handle handle = hit.Primitive == 1 ? spheres.Get((int)hit.Index) : planes.Get((int)hit.Index);
        Component? owner = registry.OwnerOf(handle);

        Logger.LogInformation("Hit {Obj}", owner?.Parent.Name ?? "unknown");
    }
}