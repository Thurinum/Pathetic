using System.Numerics;

namespace Pathetic.Physics;

// Port implemented by a physics middleware adapter. The scene graph only ever talks to this interface,
// so a different engine can be swapped in by providing another implementation.
public interface IPhysicsWorld
{
    BodyHandle CreateBody(in BodyDesc desc);
    void DestroyBody(BodyHandle handle);
    bool IsValid(BodyHandle handle);

    Pose GetPose(BodyHandle handle);
    void SetPose(BodyHandle handle, in Pose pose);

    BodyKind GetKind(BodyHandle handle);
    void SetKind(BodyHandle handle, BodyKind kind);

    void SetVelocity(BodyHandle handle, Vector3 linear, Vector3 angular = default);

    void StepSimulation(float fixedDeltaTime);
}
