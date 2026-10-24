using System.Numerics;
using Pathetic.SceneGraph;
using Pathetic.SceneGraph.Components;

namespace Pathetic.App.Components.Camera;

public class CameraComponent(CameraProvider provider) : Component
{
    private Transform _transform = null!;
    public float FovDegrees { get; } = 80.0f;
    public float FocusDistance { get; set; } = 5.0f;
    public float ApertureRadius { get; set; } = 0.15f;

    protected override void Setup()
    {
        _transform = Parent.GetComponent<Transform>();
    }

    protected override void LateUpdate(float deltaTime)
    {
        float fovRadians = float.DegreesToRadians(FovDegrees);
        float tanHalfFovRad = MathF.Tan(fovRadians * 0.5f);
        
        CameraData cameraData = new()
        {
            Origin = _transform.Position,
            TanHalfFOV = tanHalfFovRad,
            ApertureRadius = ApertureRadius,
            FocusDistance = FocusDistance,
            
            // +X right, +Y up, -Z forward, right-handed 
            Right = Vector3.Transform(Vector3.UnitX, _transform.Rotation),
            Up = Vector3.Transform(Vector3.UnitY, _transform.Rotation),
            Forward = Vector3.Transform(-Vector3.UnitZ, _transform.Rotation),
        };
        
        provider.Set(cameraData);
    }
}