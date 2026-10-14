namespace Pathetic.Physics;

// Tracks the live RigidBody components so the PhysicsSystem can push/sync them each frame.
public sealed class PhysicsRegistry
{
    private readonly List<RigidBody> _bodies = [];

    internal void Register(RigidBody body) => _bodies.Add(body);

    internal void Unregister(RigidBody body) => _bodies.Remove(body);

    internal void PushAll()
    {
        foreach (RigidBody body in _bodies)
            body.Push();
    }

    internal void PullAll()
    {
        foreach (RigidBody body in _bodies)
            body.Pull();
    }
}
