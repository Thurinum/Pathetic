using System.Numerics;
using Pathetic.SceneGraph;

namespace Pathetic.Physics;

// Simulated body attached to a SceneObject. Physics owns the Transform of dynamic bodies; static and
// kinematic bodies follow whatever is written into their Transform.
public sealed class RigidBody(IPhysicsWorld world, PhysicsRegistry registry) : Component
{
    private BodyKind _kind = BodyKind.Dynamic;

    public float Mass { get; set; } = 1f;
    public float Friction { get; set; } = 0.3f;
    public float Restitution { get; set; }
    public bool AffectedByGravity { get; set; } = true;

    public BodyHandle Handle { get; private set; } = BodyHandle.None;

    public BodyKind Kind
    {
        get => _kind;
        set
        {
            _kind = value;
            if (world.IsValid(Handle))
                world.SetKind(Handle, value);
        }
    }

    public override void Start()
    {
        Transform transform = Parent.GetComponent<Transform>();

        List<Shape> shapes = [];
        foreach (Collider collider in Parent.GetComponents<Collider>())
            shapes.Add(collider.BuildShape());

        BodyDesc desc = new()
        {
            Kind = _kind,
            Pose = new Pose(transform.Position, transform.Rotation),
            Mass = Mass,
            Friction = Friction,
            Restitution = Restitution,
            AffectedByGravity = AffectedByGravity,
            Shapes = shapes
        };

        Handle = world.CreateBody(desc);
        registry.Register(this);
    }

    public override void OnDestroy()
    {
        registry.Unregister(this);

        if (world.IsValid(Handle))
            world.DestroyBody(Handle);

        Handle = BodyHandle.None;
    }

    // Moves the body (and its Transform) immediately, clearing any accumulated velocity.
    public void Teleport(Vector3 position, Quaternion rotation)
    {
        if (!world.IsValid(Handle))
            return;

        world.SetPose(Handle, new Pose(position, rotation));
        world.SetVelocity(Handle, Vector3.Zero);
        Parent.Transform.Position = position;
        Parent.Transform.Rotation = rotation;
    }

    public void ZeroVelocity()
    {
        if (world.IsValid(Handle))
            world.SetVelocity(Handle, Vector3.Zero);
    }

    // Pushes externally driven poses into the world. Dynamic bodies are left alone so the solver stays authoritative.
    internal void Push()
    {
        if (!world.IsValid(Handle) || _kind is BodyKind.Dynamic)
            return;

        Transform transform = Parent.Transform;
        world.SetPose(Handle, new Pose(transform.Position, transform.Rotation));
    }

    // Reads solver-driven poses back into the Transform so the renderer picks them up in LateUpdate.
    internal void Pull()
    {
        if (!world.IsValid(Handle) || _kind is not BodyKind.Dynamic)
            return;

        Pose pose = world.GetPose(Handle);
        Parent.Transform.Position = pose.Position;
        Parent.Transform.Rotation = pose.Rotation;
    }
}
