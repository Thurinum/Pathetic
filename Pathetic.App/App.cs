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
    public void Run()
    {
        var scene = sceneManager.CreateScene();

        sceneManager.CreateObject(scene, "fpsCounter")
            .AddComponent<FPSCounter>();

        sceneManager.CreateObject(scene, "youssefAimePasTemporalAccumFix")
            .AddComponent<RenderSettingsComponent>();

        var floor = AddPlane(scene, new Vector3(0.0f, -0.5f, 0.0f), Quaternion.FromEuler(new Vector3(-90.0f, 0.0f, 0.0f)));
        floor.Width = 500.0f;
        floor.Height = 500.0f;
        floor.Material = new Material { Albedo = Rgb(0.6f, 0.6f, 0.6f) };

        AddSphere(scene, new Vector3(2.0f, -0.2f, 0.0f), 0.7f);
        AddSphere(scene, new Vector3(-1.3f, -0.3f, 0.9f), 0.2f);
        AddSphere(scene, new Vector3(-0.2f, 1.0f, 0.0f), 0.7f, Rgb(0.0f, 0.0f, 0.0f), Rgb(1.0f, 0.8f, 0.6f), 15.0f);
        AddModel(scene, "cube.obj", new Vector3(0.5f, 0.0f, 1.5f), 0.5f);
        AddModel(scene, "teapot.obj", new Vector3(-2.2f, -0.5f, 3.2f), 1f, Quaternion.CreateFromAxisAngle(Vector3.UnitY, -0.35f));
        AddModel(scene, "cube.obj", new Vector3(1.4f, -0.35f, 1.0f), 0.15f, Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.6f), keepLocalGeometry: true);
        
        var camera = sceneManager.CreateObject(scene, "camera");
        camera.AddComponent<CameraComponent>();
        var t = camera.GetComponent<Transform>();
        t.Position = new Vector3(0, 5, 10);

        // for (int i = 0; i < 100; i++)
        // {
        //     AddCircle(scene, Random.Shared.NextSingle() * (150 - 50), Random.Shared.NextSingle(), Random.Shared.NextSingle());
        // }

        var orbit = camera.AddComponent<CameraControls>();
        orbit.Target = new Vector3(2,0,0);
        
        // AddPlane(scene, new Vector3( 0, -1,  0), Quaternion.FromEuler(new Vector3(-90,   0,  0))); // floor  +Y
        // AddPlane(scene, new Vector3( 0,  1,  0), Quaternion.FromEuler(new Vector3( 90,   0,  0))); // ceil   -Y
        // AddPlane(scene, new Vector3(-1,  0,  0), Quaternion.FromEuler(new Vector3(  0,  90,  0))); // left   +X
        // AddPlane(scene, new Vector3( 1,  0,  0), Quaternion.FromEuler(new Vector3(  0, -90,  0))); // right  -X
        // AddPlane(scene, new Vector3( 0,  0, -1), Quaternion.FromEuler(new Vector3(  0,   0,  0))); // back   +Z
        
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

        // obj.AddComponent<RandomMover>();

        return plane;
    }

    private void AddModel(
        Scene scene,
        string model,
        Vector3 position,
        float scale,
        Quaternion? rotation = null,
        bool keepLocalGeometry = false)
    {
        var obj = sceneManager.CreateObject(scene, model);
        obj.Transform.Position = position;
        obj.Transform.Scale = new Vector3(scale);

        if (rotation is { } orientation)
            obj.Transform.Rotation = orientation;

        var mesh = obj.AddComponent<MeshComponent>();
        mesh.KeepLocalGeometry = keepLocalGeometry;

        objLoader.Initialize(mesh, model);
        mesh.Material = new Material { Albedo = RandomColor() };
    }

    private static RgbaByte RandomColor() =>
        new((byte)Random.Shared.Next(64, 256), (byte)Random.Shared.Next(64, 256), (byte)Random.Shared.Next(64, 256), 255);

    private static RgbaByte Rgb(float r, float g, float b) =>
        new((byte)MathF.Round(r * 255.0f), (byte)MathF.Round(g * 255.0f), (byte)MathF.Round(b * 255.0f), 255);

    private void AddSphere(
        Scene scene,
        Vector3 position,
        float radius,
        RgbaByte albedo = default,
        RgbaByte emission = default,
        float emissionStrength = 1.0f)
    {
        var obj = sceneManager.CreateObject(scene, "sphere");

        obj.Transform.Position = position;

        var sphere = obj.AddComponent<SphereComponent>();
        sphere.Radius = radius;
        sphere.Material = new Material
        {
            Albedo = albedo == default ? RandomColor() : albedo,
            Emission = emission,
            EmissionStrength = emissionStrength
        };
    }
    
    private void AddCircle(Scene scene, float r, float x, float y)
    {
        var obj = sceneManager.CreateObject(scene, "circle");

        obj.Transform.Position = new Vector3(x, y, 0.0f);
        
        var circle = obj.AddComponent<CircleComponent>();
        circle.Radius = r;
        
        var mover = obj.AddComponent<RandomMover>();
        mover.Speed = 0.2f;
    }
}