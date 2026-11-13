using ImGuiNET;
using Pathetic.App.Components.Camera;
using Pathetic.App.Input;
using Pathetic.App.RenderPasses;
using Pathetic.Renderer.Buffers;
using Pathetic.Renderer.Utils;
using Pathetic.SceneGraph;
using Pathetic.SceneGraph.Components;

namespace Pathetic.App.Components.GUI;

public class EditorSelection(InputActions input, RaycastService raycast, CameraProvider camera,
    PrimitiveRegistry registry,
    SparseSet<SpherePrimitive> spheres,
    SparseSet<PlanePrimitive> planes) : Component
{
    private HashSet<Component> _selectionSet = [];
    private bool _isToggle = false;
    
    protected override void Setup()
    {
        input.EditorViewport.Select.Triggered += () => TrySelect(toggle: false);
        input.EditorViewport.ToggleSelect.Triggered += () => TrySelect(toggle: true); 
        raycast.ResultReceived += Select;
    }

    private void TrySelect(bool toggle)
    {
        _isToggle = toggle;
        Ray ray = camera.ScreenToCamera(input.EditorViewport.MousePosition.Value.XY);
        raycast.Raycast(ray);
    }

    private int _selectedItem = 0;
    protected override void GUI()
    {
        string[] names = [.. _selectionSet.Select(s => s.GetType().Name)];
        ImGui.ListBox("Selections", ref _selectedItem, names, _selectionSet.Count);
    }

    private void Select(HitResult hit)
    {
        if (!hit.IsHit)
        {
            _selectionSet.Clear();
            return;
        }

        bool isSphere = hit.Primitive == 1;
        Type primitiveType = isSphere ? typeof(SpherePrimitive) : typeof(PlanePrimitive);
        Handle handle = isSphere ? spheres.Get((int)hit.Index) : planes.Get((int)hit.Index);
        Component obj = registry.OwnerOf(primitiveType, handle)!;

        if (_isToggle)
        {
            if (!_selectionSet.Remove(obj))
            {
                _selectionSet.Add(obj);
            }
        }
        else
        {
            _selectionSet.Add(obj);
        }
    }
}