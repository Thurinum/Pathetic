using System.Numerics;
using Microsoft.Extensions.Options;
using PathTracerCore.SceneGraph;

namespace PathTracerCore.Renderer.Camera;

public class CameraComponent(CameraState view, IOptions<EngineOptions> options) : Component
{
    private Transform _transform = null!;
    public float FovDegrees { get; } = 80.0f;
    public float FocusDistance = 5.0f;
    public float ApertureRadius = 0.15f;

    public override void Awake()
    {
        _transform = Parent.GetComponent<Transform>();
    }

    public override void LateUpdate(float deltaTime)
    {
        float aspectRatio = (float)options.Value.WindowWidth / options.Value.WindowHeight;
        float fovRadians = MathF.PI / 180.0f * FovDegrees;
        float tanHalfFovRad = MathF.Tan(fovRadians * 0.5f);
        
        CameraData cameraData = new()
        {
            Origin = _transform.Position,
            AspectRatio = aspectRatio,
            TanHalfFOV = tanHalfFovRad,
            ApertureRadius = ApertureRadius,
            FocusDistance = FocusDistance,
            
            // +X right, +Y up, -Z forward, right-handed 
            Right = Vector3.Transform(Vector3.UnitX, _transform.Rotation),
            Up = Vector3.Transform(Vector3.UnitY, _transform.Rotation),
            Forward = Vector3.Transform(-Vector3.UnitZ, _transform.Rotation),
        };
        
        view.Update(cameraData);
    }
}