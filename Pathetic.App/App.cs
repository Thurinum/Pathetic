using System.Numerics;
using NeoVeldrid;
using Pathetic.App.Assets;
using Pathetic.App.Components;
using Pathetic.App.Components.Camera;
using Pathetic.App.Components.GUI;
using Pathetic.Renderer.Utils;
using Pathetic.SceneGraph;
using Pathetic.SceneGraph.Scenes;

namespace Pathetic.App;

public sealed class App(SceneLoop loop, SceneManager sceneManager, ObjLoader objLoader)
{
    private static readonly Random ColorRng = new(1234);

    public void Run()
    {
        var scene = sceneManager.CreateScene();

        sceneManager.CreateObject(scene, "fpsCounter")
            .AddComponent<FPSCounter>();
        
        sceneManager.CreateObject(scene, "renderSettings")
            .AddComponent<RenderSettingsComponent>();
        
        sceneManager.CreateObject(scene, "editor")
            .AddComponent<EditorSelection>();

        var floor = AddPlane(scene, new Vector3(0.0f, -0.5f, 0.0f), Quaternion.FromEuler(new Vector3(-90.0f, 0.0f, 0.0f)));
        floor.Width = 500.0f;
        floor.Height = 500.0f;
        floor.Material = Material.Default with { Albedo = Rgb(0.6f, 0.6f, 0.6f) };

        AddSphere(scene, new Vector3(2.0f, -0.2f, 0.0f), 0.7f);
        AddSphere(scene, new Vector3(-1.3f, -0.3f, 0.9f), 0.2f);
        AddSphere(scene, new Vector3(-0.2f, 1.0f, 0.0f), 0.7f, Rgb(0.0f, 0.0f, 0.0f), Rgb(1.0f, 0.8f, 0.6f), 15.0f);
        AddModel(scene, "cube.obj", new Vector3(0.5f, 0.0f, 1.5f), 0.5f, albedo: RandomColor());
        AddModel(scene, "teapot.obj", new Vector3(-2.2f, -0.5f, 3.2f), 1f, Quaternion.CreateFromAxisAngle(Vector3.UnitY, -0.35f), RandomColor());
        AddModel(scene, "cow.obj", new Vector3(2.2f, 1.3f, -3.2f), 0.5f, Quaternion.CreateFromAxisAngle(Vector3.UnitY, -0.35f), RandomColor());
        AddModel(scene, "cube.obj", new Vector3(1.4f, -0.35f, 1.0f), 0.15f, Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.6f), RandomColor());

        var camera = sceneManager.CreateObject(scene, "camera");
        camera.AddComponent<CameraComponent>();
        var t = camera.GetComponent<Transform>();
        t.Position = new Vector3(0, 5, 10);

        var orbit = camera.AddComponent<CameraControls>();
        orbit.Target = new Vector3(2,0,0);

        loop.Run(scene);
    }
    
    private PlaneComponent AddPlane(Scene scene, Vector3 pos, Quaternion rot)
    {
        var obj = sceneManager.CreateObject(scene, "plane");
        var trans = obj.GetComponent<Transform>();
        trans.Position = pos;
        trans.Rotation = rot;
        
        var plane = obj.AddComponent<PlaneComponent>();
        plane.Width = 2f;
        plane.Height = 2f;

        return plane;
    }

    private void AddModel(
        Scene scene,
        string model,
        Vector3 position,
        float scale,
        Quaternion? rotation = null,
        RgbaByte? albedo = null)
    {
        var obj = sceneManager.CreateObject(scene, model);
        obj.Transform.Position = position;
        obj.Transform.Scale = new Vector3(scale);

        if (rotation is { } orientation)
            obj.Transform.Rotation = orientation;

        MeshAsset asset = objLoader.Load(model);

        if (albedo is { } color)
            asset = asset with { Material = asset.Material with { Albedo = color } };

        var mesh = obj.AddComponent<MeshComponent>();
        mesh.Asset = asset;
    }

    private static RgbaByte RandomColor() =>
        new((byte)ColorRng.Next(64, 256), (byte)ColorRng.Next(64, 256), (byte)ColorRng.Next(64, 256), 255);

    private static RgbaByte Rgb(float r, float g, float b) =>
        new((byte)MathF.Round(r * 255.0f), (byte)MathF.Round(g * 255.0f), (byte)MathF.Round(b * 255.0f), 255);

    private void AddSphere(
        Scene scene,
        Vector3 position,
        float radius,
        RgbaByte? albedo = null,
        RgbaByte? emission = null,
        float emissionStrength = 1.0f)
    {
        var obj = sceneManager.CreateObject(scene, "sphere");

        obj.Transform.Position = position;

        var sphere = obj.AddComponent<SphereComponent>();
        sphere.Radius = radius;
        sphere.Material = Material.Default with
        {
            Albedo = albedo ?? RandomColor(),
            Emission = emission.GetValueOrDefault(),
            EmissionStrength = emissionStrength
        };
    }
}
