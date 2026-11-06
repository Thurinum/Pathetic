using System.Numerics;
using ImGuiNET;
using Pathetic.App.Components.Camera;
using Pathetic.App.Input;
using Pathetic.SceneGraph;
using Pathetic.SceneGraph.Components;

namespace Pathetic.App.Components.GUI;

public class CameraControls(InputActions inputActions) : Component
{
    private const float MinDistance = 0.1f;
    private const float MaxDistance = 100.0f;
    private const float MaxElevation = 89.9f;

    private Transform _transform = null!;
    private CameraComponent _camera = null!;
    
    public Vector3 Target { get; set; } = Vector3.Zero;
    public float Distance { get; set; } = 5.0f;
    public float AzimuthDegrees { get; set; }
    public float ElevationDegrees { get; set; }
    public float Speed { get; set; }

    protected override void Setup()
    {
        _transform = Parent.GetComponent<Transform>();
        _camera = Parent.GetComponent<CameraComponent>();
    }

    protected override void Update(float deltaTime)
    {
        Vector2 orbit = inputActions.EditorViewport.Orbit.Value;
        AzimuthDegrees -= orbit.X;
        ElevationDegrees = Math.Clamp(ElevationDegrees - orbit.Y, -MaxElevation, MaxElevation);

        float zoom = inputActions.EditorViewport.Zoom.Value;
        Distance = Math.Clamp(Distance * MathF.Exp(-zoom), MinDistance, MaxDistance);

        AzimuthDegrees += Speed * deltaTime;
        
        float a = float.DegreesToRadians(AzimuthDegrees);
        float e = float.DegreesToRadians(ElevationDegrees);
        
        // unit circle
        Vector3 dir = new(
            MathF.Cos(e) * MathF.Sin(a), 
            MathF.Sin(e), 
            MathF.Cos(e) * MathF.Cos(a));
        _transform.Position = Target + dir * Distance;
        
        Vector3 forward = Vector3.Normalize(Target - _transform.Position);
        Matrix4x4 world = Matrix4x4.CreateWorld(_transform.Position, forward, Vector3.UnitY);
        _transform.Rotation = Quaternion.CreateFromRotationMatrix(world);
    }

    protected override void GUI()
    {
        ImGui.SetNextWindowSize(new(500, 0), ImGuiCond.Always);
        ImGui.Begin("Camera");
        
        Vector3 target = Target;
        if (ImGui.DragFloat3("Target", ref target, 0.05f))
            Target = target;

        float azimuth = AzimuthDegrees;
        if (ImGui.SliderFloat("Azimuth", ref azimuth, 0, 360))
            AzimuthDegrees = azimuth;

        float elevation = ElevationDegrees;
        if (ImGui.SliderFloat("Elevation", ref elevation, -MaxElevation, MaxElevation))
            ElevationDegrees = elevation;
        
        float distance = Distance;
        if (ImGui.SliderFloat("Distance", ref distance, 1, MaxDistance))
            Distance = distance;
        
        float speed = Speed;
        if (ImGui.SliderFloat("Speed", ref speed, -250, 250))
            Speed = speed;

        float focusDistance = _camera.FocusDistance;
        if (ImGui.SliderFloat("Focus Distance", ref focusDistance, 0.0f, 20.0f))
            _camera.FocusDistance = focusDistance;

        float apertureRadius = _camera.ApertureRadius;
        if (ImGui.SliderFloat("Aperture Radius", ref apertureRadius, 0.0f, 0.5f))
            _camera.ApertureRadius = apertureRadius;

        ImGui.End();
    }
}
