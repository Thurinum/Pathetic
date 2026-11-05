using System.Numerics;
using NeoVeldrid;
using Pathetic.App.Components;
using Pathetic.App.Components.Camera;
using Pathetic.App.Components.GUI;
using Pathetic.Renderer.Utils;
using Pathetic.SceneGraph;
using Pathetic.SceneGraph.Scenes;

namespace Pathetic.App;

public class App(SceneLoop loop, SceneManager sceneManager)
{
    public void Run()
    {
        var scene = sceneManager.CreateScene();

        sceneManager.CreateObject(scene, "fpsCounter")
            .AddComponent<FPSCounter>();

        sceneManager.CreateObject(scene, "youssefAimePasTemporalAccumFix")
            .AddComponent<RenderSettingsComponent>();

        AddSphere(scene, new Vector3(2.0f, -100.5f, 0.0f), 100.0f, Rgb(0.6f, 0.6f, 0.6f));
        AddSphereGrid(scene, new Vector3(2, 0, 0), 24, 0.58f);

        AddSphere(scene, new Vector3(-3, 5, -3), 0.5f, Rgb(0, 0, 0), Rgb(1.0f, 0.15f, 0.1f), 50.0f);
        AddSphere(scene, new Vector3(7, 5, -3), 0.5f, Rgb(0, 0, 0), Rgb(0.15f, 0.85f, 0.25f), 50.0f);
        AddSphere(scene, new Vector3(-3, 5, 3), 0.5f, Rgb(0, 0, 0), Rgb(0.15f, 0.4f, 1.0f), 50.0f);
        AddSphere(scene, new Vector3(7, 5, 3), 0.5f, Rgb(0, 0, 0), Rgb(1.0f, 0.7f, 0.2f), 50.0f);

        var camera = sceneManager.CreateObject(scene, "camera");
        camera.AddComponent<CameraComponent>();
        var t = camera.GetComponent<Transform>();
        t.Position = new Vector3(0, 5, 10);

        // for (int i = 0; i < 100; i++)
        // {
        //     AddCircle(scene, Random.Shared.NextSingle() * (150 - 50), Random.Shared.NextSingle(), Random.Shared.NextSingle());
        // }

        var orbit = camera.AddComponent<CameraControls>();
        orbit.Target = new Vector3(2, 0, 0);
        orbit.Distance = 15.0f;
        orbit.ElevationDegrees = 28.0f;
        
        AddPlane(scene, new Vector3( 0, -1,  0), Quaternion.FromEuler(new Vector3(-90,   0,  0))); // floor  +Y
        AddPlane(scene, new Vector3( 0,  1,  0), Quaternion.FromEuler(new Vector3( 90,   0,  0))); // ceil   -Y
        AddPlane(scene, new Vector3(-1,  0,  0), Quaternion.FromEuler(new Vector3(  0,  90,  0))); // left   +X
        AddPlane(scene, new Vector3( 1,  0,  0), Quaternion.FromEuler(new Vector3(  0, -90,  0))); // right  -X
        AddPlane(scene, new Vector3( 0,  0, -1), Quaternion.FromEuler(new Vector3(  0,   0,  0))); // back   +Z
        
        loop.Run(scene);
    }
    
    private void AddSphereGrid(Scene scene, Vector3 center, int size, float spacing)
    {
        RgbaByte[] palette =
        [
            Rgb(0.90f, 0.35f, 0.25f),
            Rgb(0.95f, 0.75f, 0.25f),
            Rgb(0.25f, 0.75f, 0.45f),
            Rgb(0.25f, 0.55f, 0.90f),
            Rgb(0.75f, 0.45f, 0.90f),
        ];

        float offset = (size - 1) * spacing * 0.5f;
        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size; x++)
            {
                uint hash = (uint)(x * 73856093 ^ z * 19349663);
                float variation = (hash & 1023) / 1023.0f;
                float radius = 0.12f + 0.10f * variation;
                float px = center.X + x * spacing - offset;
                float pz = center.Z + z * spacing - offset;

                AddSphere(
                    scene,
                    new Vector3(px, center.Y + radius, pz),
                    radius,
                    palette[(x + z) % palette.Length]);
            }
        }
    }

    private void AddPlane(Scene scene, Vector3 pos, Quaternion rot)
    {
        var obj = sceneManager.CreateObject(scene, "plane");
        var trans = obj.GetComponent<Transform>();
        trans.Position = pos;
        trans.Rotation = rot;
        
        var plane = obj.AddComponent<PlaneComponent>();
        plane.Width = 2f;
        plane.Height = 2f;

        // obj.AddComponent<RandomMover>();
    }

    private static RgbaByte Rgb(float r, float g, float b) =>
        new((byte)MathF.Round(r * 255.0f), (byte)MathF.Round(g * 255.0f), (byte)MathF.Round(b * 255.0f), 255);

    private void AddSphere(
        Scene scene,
        Vector3 position,
        float radius,
        RgbaByte albedo,
        RgbaByte emission = default,
        float emissionStrength = 1.0f)
    {
        var obj = sceneManager.CreateObject(scene, "sphere");

        obj.Transform.Position = position;

        var sphere = obj.AddComponent<SphereComponent>();
        sphere.Radius = radius;
        sphere.Albedo = albedo;
        sphere.Emission = emission;
        sphere.EmissionStrength = emissionStrength;
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