using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Jitter2;
using Jitter2.LinearMath;

using JitterBody = Jitter2.Dynamics.RigidBody;
using JitterMotionType = Jitter2.Dynamics.MotionType;
using JitterMassMode = Jitter2.Dynamics.MassInertiaUpdateMode;
using JitterShape = Jitter2.Collision.Shapes.RigidBodyShape;
using JitterSphere = Jitter2.Collision.Shapes.SphereShape;
using JitterBox = Jitter2.Collision.Shapes.BoxShape;
using JitterCapsule = Jitter2.Collision.Shapes.CapsuleShape;

namespace Pathetic.Physics.Jitter;

// IPhysicsWorld implemented on top of Jitter Physics 2. This is the only place in the engine that
// knows Jitter exists; everything above it talks to the Pathetic.Physics port.
public sealed class JitterPhysicsWorld : IPhysicsWorld, IDisposable
{
    private readonly World _world = new();
    private readonly List<JitterBody?> _bodies = [];
    private readonly List<int> _generations = [];
    private readonly Stack<int> _free = [];

    public JitterPhysicsWorld()
    {
        _world.Gravity = new JVector(0f, -9.81f, 0f);
    }

    public BodyHandle CreateBody(in BodyDesc desc)
    {
        int index;
        if (_free.Count > 0)
        {
            index = _free.Pop();
        }
        else
        {
            index = _bodies.Count;
            _bodies.Add(null);
            _generations.Add(0);
        }

        JitterBody body = _world.CreateRigidBody();
        body.Position = ToJitter(desc.Pose.Position);
        body.Orientation = ToJitter(desc.Pose.Rotation);
        body.Velocity = ToJitter(desc.LinearVelocity);
        body.Friction = desc.Friction;
        body.Restitution = desc.Restitution;
        body.AffectedByGravity = desc.AffectedByGravity;

        foreach (Shape shape in desc.Shapes)
            body.AddShape(CreateShape(shape), JitterMassMode.Preserve);

        switch (desc.Kind)
        {
            case BodyKind.Static:
                body.MotionType = JitterMotionType.Static;
                break;
            case BodyKind.Kinematic:
                body.MotionType = JitterMotionType.Kinematic;
                break;
            default:
                body.MotionType = JitterMotionType.Dynamic;
                body.SetMassInertia(desc.Mass > 0f ? desc.Mass : 1f);
                break;
        }

        _bodies[index] = body;
        return new BodyHandle(index, _generations[index]);
    }

    public void DestroyBody(BodyHandle handle)
    {
        if (!TryResolve(handle, out JitterBody? body))
            return;

        _world.Remove(body);
        _bodies[handle.Index] = null;
        _generations[handle.Index]++;
        _free.Push(handle.Index);
    }

    public bool IsValid(BodyHandle handle) => TryResolve(handle, out _);

    public Pose GetPose(BodyHandle handle)
    {
        JitterBody body = Resolve(handle);
        return new Pose(FromJitter(body.Position), FromJitter(body.Orientation));
    }

    public void SetPose(BodyHandle handle, in Pose pose)
    {
        JitterBody body = Resolve(handle);
        body.Position = ToJitter(pose.Position);
        body.Orientation = ToJitter(pose.Rotation);
    }

    public BodyKind GetKind(BodyHandle handle) => Resolve(handle).MotionType switch
    {
        JitterMotionType.Static => BodyKind.Static,
        JitterMotionType.Kinematic => BodyKind.Kinematic,
        _ => BodyKind.Dynamic
    };

    public void SetKind(BodyHandle handle, BodyKind kind)
    {
        JitterBody body = Resolve(handle);
        body.MotionType = kind switch
        {
            BodyKind.Static => JitterMotionType.Static,
            BodyKind.Kinematic => JitterMotionType.Kinematic,
            _ => JitterMotionType.Dynamic
        };
    }

    public void SetVelocity(BodyHandle handle, Vector3 linear, Vector3 angular = default)
    {
        JitterBody body = Resolve(handle);
        body.Velocity = ToJitter(linear);
        body.AngularVelocity = ToJitter(angular);
    }

    public void StepSimulation(float fixedDeltaTime) => _world.Step(fixedDeltaTime, false);

    public void Dispose() => _world.Dispose();

    private bool TryResolve(BodyHandle handle, [NotNullWhen(true)] out JitterBody? body)
    {
        if (handle.Index < 0 || handle.Index >= _bodies.Count || _generations[handle.Index] != handle.Generation)
        {
            body = null;
            return false;
        }

        body = _bodies[handle.Index];
        return body is not null;
    }

    private JitterBody Resolve(BodyHandle handle) =>
        TryResolve(handle, out JitterBody? body)
            ? body
            : throw new InvalidOperationException($"Unknown or stale body handle {handle}.");

    private static JitterShape CreateShape(Shape shape) => shape switch
    {
        SphereShape sphere => new JitterSphere(sphere.Radius),
        BoxShape box => new JitterBox(box.Size.X, box.Size.Y, box.Size.Z),
        CapsuleShape capsule => new JitterCapsule(capsule.Radius, capsule.Length),
        _ => throw new NotSupportedException($"Unsupported shape {shape.GetType().Name}.")
    };

    private static JVector ToJitter(Vector3 value) => new(value.X, value.Y, value.Z);

    private static Vector3 FromJitter(in JVector value) => new(value.X, value.Y, value.Z);

    private static JQuaternion ToJitter(Quaternion value) => new(value.X, value.Y, value.Z, value.W);

    private static Quaternion FromJitter(in JQuaternion value) => new(value.X, value.Y, value.Z, value.W);
}
