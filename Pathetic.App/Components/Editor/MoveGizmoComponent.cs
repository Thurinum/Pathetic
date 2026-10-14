using System.Numerics;
using ImGuiNET;
using Pathetic.App.Components.Camera;
using Pathetic.Physics;
using Pathetic.Renderer.Graph;
using Pathetic.SceneGraph;

namespace Pathetic.App.Components.Editor;

// Blender-style modal move controls:
//   right click        select the sphere under the cursor (empty space deselects)
//   G                  grab; mouse movement moves the selection freely on the view plane
//   X / Y / Z          constrain movement to a world axis (a line is drawn)
//   Shift + X / Y / Z  constrain movement to a plane (two lines are drawn)
//   left click, Enter  confirm
//   right click, Esc   cancel and restore the original position
//
// Selection itself is published through SelectionState; the highlight is drawn by the renderer
// (Lambert preview in PathTracer + an outline from the object id buffer in SelectionOutlinePass).
public sealed class MoveGizmoComponent(CameraProvider camera, SceneRevision revision, SelectionState selection) : Component
{
    private const uint XAxisColor = 0xFF0000FF;
    private const uint YAxisColor = 0xFF00FF00;
    private const uint ZAxisColor = 0xFFFF0000;
    private const uint MarkerColor = 0xFF3399FF; // Blender-ish selection orange, ImGui ABGR
    private const float LineThickness = 1.5f;
    private const float LineExtentPixels = 8192f;

    private SceneObject? _selected;

    private bool _isMoving;
    private Constraint _constraint;

    private Vector3 _originalPosition;
    private Vector3 _anchorPosition;
    private Vector3 _grabPoint;
    private float _grabParameter;

    private RigidBody? _grabbedBody;
    private BodyKind _grabbedOriginalKind;
    private Quaternion _originalRotation;

    public override void OnGUI()
    {
        CameraData view = camera.Data;
        ImGuiIOPtr io = ImGui.GetIO();
        Vector2 displaySize = io.DisplaySize;
        Vector2 mouse = ImGui.GetMousePos();

        // TEMP DEBUG: remove
        if (_selected is null && Root.Objects.FindLast(o => o.TryGetComponent<SphereComponent>(out _)) is { } probe)
            SetSelected(probe);
        ImGui.GetForegroundDrawList().AddText(new Vector2(20f, 60f), 0xFFFFFFFFu, $"DBG shift={io.KeyShift} moving={_isMoving} constraint={_constraint} sel={selection.SelectedObjectId}");

        if (_isMoving)
            UpdateMove(view, displaySize, mouse, io);
        else
            UpdateSelection(view, displaySize, mouse, io);

        ImDrawListPtr drawList = ImGui.GetForegroundDrawList();
        if (_isMoving)
            DrawConstraint(drawList, view, displaySize);

        DrawMarker(drawList, view, displaySize);
    }

    private void UpdateSelection(CameraData view, Vector2 displaySize, Vector2 mouse, ImGuiIOPtr io)
    {
        if (!io.WantCaptureMouse && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
        {
            SceneObject? hit = null;
            if (EditorCamera.TryBuildRay(view, mouse, displaySize, out ViewRay ray))
                ScenePicker.TryPickSphere(Root, ray, out hit);

            SetSelected(hit);
        }

        if (_selected is not null && !io.WantCaptureKeyboard && ImGui.IsKeyPressed(ImGuiKey.G))
            BeginMove(view, displaySize, mouse);
    }

    private void SetSelected(SceneObject? target)
    {
        if (ReferenceEquals(_selected, target))
            return;

        _selected = target;
        selection.SelectedObjectId = target?.Id ?? 0u;
        revision.Invalidate();
    }

    private void BeginMove(CameraData view, Vector2 displaySize, Vector2 mouse)
    {
        if (_selected is null)
            return;

        _isMoving = true;
        _constraint = Constraint.Free;
        _originalPosition = _selected.Transform.Position;
        _anchorPosition = _originalPosition;
        _originalRotation = _selected.Transform.Rotation;
        BeginGrabBody(_selected);

        if (EditorCamera.TryBuildRay(view, mouse, displaySize, out ViewRay ray))
            Reanchor(view, ray);
    }

    // Hands a dynamic body over to the gizmo: while grabbed it becomes kinematic so the solver stops
    // fighting the cursor, but it still pushes other dynamic bodies around.
    private void BeginGrabBody(SceneObject target)
    {
        _grabbedBody = null;

        if (!target.TryGetComponent<RigidBody>(out RigidBody? body) || body is null)
            return;

        _grabbedBody = body;
        _grabbedOriginalKind = body.Kind;
        body.Kind = BodyKind.Kinematic;
        body.ZeroVelocity();
    }

    private void UpdateMove(CameraData view, Vector2 displaySize, Vector2 mouse, ImGuiIOPtr io)
    {
        bool hasRay = EditorCamera.TryBuildRay(view, mouse, displaySize, out ViewRay ray);
        UpdateConstraint(io, view, ray, hasRay);

        if (hasRay)
            ApplyPosition(ResolvePosition(view, ray));

        bool confirm = ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter);
        if (!confirm && !io.WantCaptureMouse)
            confirm = ImGui.IsMouseClicked(ImGuiMouseButton.Left);

        if (confirm)
        {
            EndMove(restore: false);
            return;
        }

        bool cancel = ImGui.IsKeyPressed(ImGuiKey.Escape);
        if (!cancel && !io.WantCaptureMouse)
            cancel = ImGui.IsMouseClicked(ImGuiMouseButton.Right);

        if (cancel)
            EndMove(restore: true);
    }

    private void EndMove(bool restore)
    {
        if (restore)
            ApplyPosition(_originalPosition, force: true);

        if (_grabbedBody is not null)
        {
            if (restore)
                _grabbedBody.Teleport(_originalPosition, _originalRotation);
            else
                _grabbedBody.ZeroVelocity();

            _grabbedBody.Kind = _grabbedOriginalKind;
            _grabbedBody = null;
        }

        _isMoving = false;
    }

    private void UpdateConstraint(ImGuiIOPtr io, CameraData view, in ViewRay ray, bool hasRay)
    {
        if (io.WantCaptureKeyboard)
            return;

        // The NeoVeldrid ImGui backend forwards Shift as ImGuiMod_Shift, not as ImGuiKey.LeftShift,
        // so IsKeyDown(ImGuiKey.LeftShift) is always false here. io.KeyShift is the reliable source.
        bool shift = io.KeyShift;

        Constraint? requested = null;
        if (ImGui.IsKeyPressed(ImGuiKey.X))
            requested = shift ? Constraint.PlaneX : Constraint.X;
        else if (ImGui.IsKeyPressed(ImGuiKey.Y))
            requested = shift ? Constraint.PlaneY : Constraint.Y;
        else if (ImGui.IsKeyPressed(ImGuiKey.Z))
            requested = shift ? Constraint.PlaneZ : Constraint.Z;

        if (requested is not Constraint value)
            return;

        // Pressing the same constraint again releases it back to free movement.
        _constraint = _constraint == value ? Constraint.Free : value;

        if (hasRay && _selected is not null)
            Reanchor(view, ray);
    }

    private void Reanchor(CameraData view, in ViewRay ray)
    {
        _anchorPosition = _selected!.Transform.Position;

        if (IsAxis(_constraint))
        {
            _grabParameter = AxisParameter(ray, _anchorPosition, AxisOf(_constraint));
        }
        else
        {
            Vector3 normal = PlaneNormalOf(view, _constraint);
            _grabPoint = TryIntersectPlane(ray, _anchorPosition, normal, out Vector3 hit) ? hit : _anchorPosition;
        }
    }

    private Vector3 ResolvePosition(CameraData view, in ViewRay ray)
    {
        if (IsAxis(_constraint))
        {
            Vector3 axis = AxisOf(_constraint);
            float parameter = AxisParameter(ray, _anchorPosition, axis);
            return _anchorPosition + (axis * (parameter - _grabParameter));
        }

        if (TryIntersectPlane(ray, _anchorPosition, PlaneNormalOf(view, _constraint), out Vector3 hit))
            return _anchorPosition + (hit - _grabPoint);

        return _anchorPosition;
    }

    private void ApplyPosition(Vector3 position, bool force = false)
    {
        if (_selected is null)
            return;

        Transform transform = _selected.Transform;
        if (!force && transform.Position == position)
            return;

        transform.Position = position;
        _grabbedBody?.Teleport(position, transform.Rotation);
        revision.Invalidate();
    }

    private void DrawMarker(ImDrawListPtr drawList, CameraData view, Vector2 displaySize)
    {
        if (_selected is null)
            return;

        if (!EditorCamera.TryProject(view, _selected.Transform.Position, displaySize, out Vector2 screen))
            return;

        drawList.AddCircleFilled(screen, 3f, MarkerColor);
        drawList.AddText(screen + new Vector2(8f, -6f), MarkerColor, _selected.Name);
    }

    private void DrawConstraint(ImDrawListPtr drawList, CameraData view, Vector2 displaySize)
    {
        if (_selected is null)
            return;

        Vector3 origin = _selected.Transform.Position;

        if (IsAxis(_constraint))
        {
            DrawAxisLine(drawList, view, displaySize, origin, AxisOf(_constraint));
            return;
        }

        if (_constraint is Constraint.PlaneX or Constraint.PlaneY or Constraint.PlaneZ)
        {
            Vector3 excluded = PlaneNormalOf(view, _constraint);
            if (excluded != Vector3.UnitX)
                DrawAxisLine(drawList, view, displaySize, origin, Vector3.UnitX);
            if (excluded != Vector3.UnitY)
                DrawAxisLine(drawList, view, displaySize, origin, Vector3.UnitY);
            if (excluded != Vector3.UnitZ)
                DrawAxisLine(drawList, view, displaySize, origin, Vector3.UnitZ);
        }
    }

    private static void DrawAxisLine(ImDrawListPtr drawList, CameraData view, Vector2 displaySize, Vector3 origin, Vector3 axis)
    {
        bool hasFrom = EditorCamera.TryProject(view, origin, displaySize, out Vector2 from);
        bool hasTo = EditorCamera.TryProject(view, origin + axis, displaySize, out Vector2 to);
        if (!hasFrom || !hasTo)
            return;

        Vector2 direction = to - from;
        if (direction.LengthSquared() < 1e-6f)
            return;

        direction = Vector2.Normalize(direction);
        drawList.AddLine(from - (direction * LineExtentPixels), from + (direction * LineExtentPixels), AxisColorOf(axis), LineThickness);
    }

    private static bool IsAxis(Constraint constraint) => constraint is Constraint.X or Constraint.Y or Constraint.Z;

    private static Vector3 AxisOf(Constraint constraint) => constraint switch
    {
        Constraint.X or Constraint.PlaneX => Vector3.UnitX,
        Constraint.Y or Constraint.PlaneY => Vector3.UnitY,
        Constraint.Z or Constraint.PlaneZ => Vector3.UnitZ,
        _ => Vector3.Zero
    };

    private static Vector3 PlaneNormalOf(in CameraData view, Constraint constraint) => constraint switch
    {
        Constraint.PlaneX => Vector3.UnitX,
        Constraint.PlaneY => Vector3.UnitY,
        Constraint.PlaneZ => Vector3.UnitZ,
        _ => view.Forward
    };

    private static uint AxisColorOf(Vector3 axis)
    {
        if (axis == Vector3.UnitX)
            return XAxisColor;
        if (axis == Vector3.UnitY)
            return YAxisColor;
        return ZAxisColor;
    }

    // Closest-point parameter on the line (point + t * axis) to the given ray.
    private static float AxisParameter(in ViewRay ray, Vector3 point, Vector3 axis)
    {
        Vector3 offset = point - ray.Origin;
        float alignment = Vector3.Dot(axis, ray.Direction);
        float denominator = 1f - (alignment * alignment);
        float alongAxis = Vector3.Dot(axis, offset);

        if (denominator < 1e-4f)
            return alongAxis; // The axis points at the camera; fall back to a stable projection.

        float alongRay = Vector3.Dot(ray.Direction, offset);
        return ((alignment * alongRay) - alongAxis) / denominator;
    }

    private static bool TryIntersectPlane(in ViewRay ray, Vector3 point, Vector3 normal, out Vector3 hit)
    {
        float denominator = Vector3.Dot(normal, ray.Direction);
        if (MathF.Abs(denominator) < 1e-4f)
        {
            hit = point;
            return false;
        }

        float distance = Vector3.Dot(normal, point - ray.Origin) / denominator;
        hit = ray.Origin + (distance * ray.Direction);
        return distance > 0f;
    }

    private enum Constraint
    {
        Free,
        X,
        Y,
        Z,
        PlaneX,
        PlaneY,
        PlaneZ
    }
}
