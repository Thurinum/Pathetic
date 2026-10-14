using Pathetic.SceneGraph;

namespace Pathetic.Physics;

// Advances the physics world on a fixed timestep and keeps body poses in sync with the scene graph.
// Add exactly one of these to a scene.
public sealed class PhysicsSystem(IPhysicsWorld world, PhysicsRegistry registry) : Component
{
    private const float FixedDeltaTime = 1f / 60f;
    private const float MaxAccumulatedTime = 0.25f;

    private float _accumulator;

    public override void Update(float deltaTime)
    {
        registry.PushAll();

        _accumulator = MathF.Min(_accumulator + deltaTime, MaxAccumulatedTime);

        while (_accumulator >= FixedDeltaTime)
        {
            world.StepSimulation(FixedDeltaTime);
            _accumulator -= FixedDeltaTime;
        }

        registry.PullAll();
    }
}
