using System.Numerics;
using Pathetic.SceneGraph;

namespace Pathetic.App.Components;

public class RandomMover : Component
{
    public float Speed { get; set; } = 1.0f;

    private Vector3 _direction;

    public override void Awake()
    {
        float z = Random.Shared.NextSingle() * 2f - 1f;
        float angle = Random.Shared.NextSingle() * MathF.Tau;
        float radius = MathF.Sqrt(1f - z * z);
        _direction = new Vector3(radius * MathF.Cos(angle), radius * MathF.Sin(angle), z);
    }

    public override void Update(float deltaTime)
    {
        var transform = Parent.GetComponent<Transform>();
        Vector3 position = transform.Position;

        position.X += _direction.X * Speed * deltaTime;
        position.Y += _direction.Y * Speed * deltaTime;
        position.Z += _direction.Z * Speed * deltaTime;

        if (position.X <= 0f)
        {
            position.X = 0f;
            _direction.X = MathF.Abs(_direction.X);
        }
        else if (position.X >= 1f)
        {
            position.X = 1f;
            _direction.X = -MathF.Abs(_direction.X);
        }

        if (position.Y <= 0f)
        {
            position.Y = 0f;
            _direction.Y = MathF.Abs(_direction.Y);
        }
        else if (position.Y >= 1f)
        {
            position.Y = 1f;
            _direction.Y = -MathF.Abs(_direction.Y);
        }

        if (position.Z <= 0f)
        {
            position.Z = 0f;
            _direction.Z = MathF.Abs(_direction.Z);
        }
        else if (position.Z >= 1f)
        {
            position.Z = 1f;
            _direction.Z = -MathF.Abs(_direction.Z);
        }

        transform.Position = position;
    }
}