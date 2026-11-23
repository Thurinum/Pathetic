using Pathetic.Renderer.Utils;
using System.Numerics;
using NeoVeldrid;
using Pathetic.App.Assets;
using Pathetic.App.Components;
using Pathetic.App.Components.Camera;
using Pathetic.SceneGraph;
using Pathetic.SceneGraph.Scenes;

namespace Pathetic.App.Scenes;

public sealed class SceneBuilder(SceneManager manager, Scene scene, ObjLoader objLoader)
{
    private const string CameraName = "camera";
    private const string SphereName = "sphere";
    private const string PlaneName = "plane";
    private const float RoomPlaneSize = 2f;

    private readonly List<SceneObject> _created = [];

    public IReadOnlyList<SceneObject> Created => _created;

    public void AddSphere(Vector3 position, float radius, RgbaByte albedo, RgbaByte emission = default, float emissionStrength = 1.0f)
    {
        var obj = Create(SphereName);
        obj.Transform.Position = position;

        var sphere = obj.AddComponent<SphereComponent>();
        sphere.Radius = radius;
        sphere.Material = Material.Default with { Albedo = albedo, Emission = emission, EmissionStrength = emissionStrength };
    }

    public void AddPlane(Vector3 position, Quaternion rotation, float size = RoomPlaneSize, RgbaByte? albedo = null)
    {
        var obj = Create(PlaneName);
        obj.Transform.Position = position;
        obj.Transform.Rotation = rotation;

        var plane = obj.AddComponent<PlaneComponent>();
        plane.Width = size;
        plane.Height = size;

        if (albedo is { } color)
            plane.Material = Material.Default with { Albedo = color };
    }

    public void AddModel(string model, Vector3 position, float scale, Quaternion? rotation = null, RgbaByte? albedo = null)
    {
        var obj = Create(model);
        obj.Transform.Position = position;
        obj.Transform.Scale = new Vector3(scale);

        if (rotation is { } orientation)
            obj.Transform.Rotation = orientation;

        MeshAsset asset = objLoader.Load(model);
        if (albedo is { } color)
            asset = asset with { Material = asset.Material with { Albedo = color } };

        obj.AddComponent<MeshComponent>().Asset = asset;
    }

    public void AddRoom()
    {
        AddPlane(new(0, -1, 0), Quaternion.FromEuler(new(-90, 0, 0)));
        AddPlane(new(0, 1, 0), Quaternion.FromEuler(new(90, 0, 0)));
        AddPlane(new(-1, 0, 0), Quaternion.FromEuler(new(0, 90, 0)));
        AddPlane(new(1, 0, 0), Quaternion.FromEuler(new(0, -90, 0)));
        AddPlane(new(0, 0, -1), Quaternion.FromEuler(new(0, 0, 0)));
    }

    internal void AddCamera(OrbitCameraDefinition definition)
    {
        var camera = Create(CameraName);
        camera.AddComponent<CameraComponent>();

        var orbit = camera.AddComponent<CameraControls>();
        orbit.Target = definition.Target;
        orbit.Distance = definition.Distance;
        orbit.AzimuthDegrees = definition.AzimuthDegrees;
        orbit.ElevationDegrees = definition.ElevationDegrees;
    }

    private SceneObject Create(string name)
    {
        SceneObject obj = manager.CreateObject(scene, name);
        _created.Add(obj);
        return obj;
    }
}
