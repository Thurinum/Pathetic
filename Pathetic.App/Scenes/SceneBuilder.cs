using System.Numerics;
using NeoVeldrid;
using Pathetic.App.Components;
using Pathetic.App.Components.Camera;
using Pathetic.Renderer.Utils;
using Pathetic.SceneGraph;
using Pathetic.SceneGraph.Scenes;

namespace Pathetic.App.Scenes;

public sealed class SceneBuilder(SceneManager manager, Scene scene)
{
    private const string CameraName = "camera";
    private const string SphereName = "sphere";
    private const string PlaneName = "plane";
    private const float RoomPlaneSize = 2f;

    public static RgbaByte Rgb(float r, float g, float b) =>
        new((byte)MathF.Round(r * 255.0f), (byte)MathF.Round(g * 255.0f), (byte)MathF.Round(b * 255.0f), 255);

    public void AddSphere(Vector3 position, float radius, RgbaByte albedo, RgbaByte emission = default, float emissionStrength = 1.0f)
    {
        var obj = manager.CreateObject(scene, SphereName);
        obj.Transform.Position = position;

        var sphere = obj.AddComponent<SphereComponent>();
        sphere.Radius = radius;
        sphere.Albedo = albedo;
        sphere.Emission = emission;
        sphere.EmissionStrength = emissionStrength;
    }

    public void AddPlane(Vector3 position, Quaternion rotation)
    {
        var obj = manager.CreateObject(scene, PlaneName);
        obj.Transform.Position = position;
        obj.Transform.Rotation = rotation;

        var plane = obj.AddComponent<PlaneComponent>();
        plane.Width = RoomPlaneSize;
        plane.Height = RoomPlaneSize;
    }

    public void AddRoom()
    {
        AddPlane(new(0, -1, 0), Quaternion.FromEuler(new(-90, 0, 0)));
        AddPlane(new(0, 1, 0), Quaternion.FromEuler(new(90, 0, 0)));
        AddPlane(new(-1, 0, 0), Quaternion.FromEuler(new(0, 90, 0)));
        AddPlane(new(1, 0, 0), Quaternion.FromEuler(new(0, -90, 0)));
        AddPlane(new(0, 0, -1), Quaternion.FromEuler(new(0, 0, 0)));
    }

    internal void AddCamera(CameraPose pose)
    {
        var camera = manager.CreateObject(scene, CameraName);
        camera.AddComponent<CameraComponent>();

        var orbit = camera.AddComponent<CameraControls>();
        orbit.Target = pose.Target;
        orbit.Distance = pose.Distance;
        orbit.AzimuthDegrees = pose.AzimuthDegrees;
        orbit.ElevationDegrees = pose.ElevationDegrees;
    }
}
