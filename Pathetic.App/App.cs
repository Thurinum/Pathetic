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
        AddSphere(scene, new Vector3(2.0f, 0.2f, 0.0f), 0.7f, Rgb(0.9f, 0.6f, 0.2f));
        AddSphere(scene, new Vector3(1.3f, -0.3f, 0.9f), 0.2f, Rgb(0.2f, 0.6f, 0.9f));
        AddSphere(scene, new Vector3(-0.2f, 1.0f, 0.0f), 0.7f, Rgb(0.0f, 0.0f, 0.0f), Rgb(1.0f, 0.8f, 0.6f), 15.0f);
        AddMesh(scene, new Vector3(0.5f, -0.5f, 1.5f), 1.0f, Rgb(0.8f, 0.8f, 0.8f));
        
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
        
        AddPlane(scene, new Vector3( 0, -1,  0), Quaternion.FromEuler(new Vector3(-90,   0,  0))); // floor  +Y
        AddPlane(scene, new Vector3( 0,  1,  0), Quaternion.FromEuler(new Vector3( 90,   0,  0))); // ceil   -Y
        AddPlane(scene, new Vector3(-1,  0,  0), Quaternion.FromEuler(new Vector3(  0,  90,  0))); // left   +X
        AddPlane(scene, new Vector3( 1,  0,  0), Quaternion.FromEuler(new Vector3(  0, -90,  0))); // right  -X
        AddPlane(scene, new Vector3( 0,  0, -1), Quaternion.FromEuler(new Vector3(  0,   0,  0))); // back   +Z
        
        loop.Run(scene);
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

    private void AddMesh(Scene scene, Vector3 position, float size, RgbaByte albedo)
    {
        var obj = sceneManager.CreateObject(scene, "mesh");
        obj.Transform.Position = position;

        var mesh = obj.AddComponent<MeshComponent>();
        mesh.Triangles = Cube(size);
        mesh.Material = new Material { Albedo = albedo };
    }

    private static MeshTriangle[] Cube(float size)
    {
        float h = size * 0.5f;

        Vector3[] corners =
        [
            new(-h, -h, -h), new(h, -h, -h), new(h, h, -h), new(-h, h, -h),
            new(-h, -h, h), new(h, -h, h), new(h, h, h), new(-h, h, h)
        ];

        int[] faces =
        [
            4, 5, 6, 4, 6, 7,
            1, 0, 3, 1, 3, 2,
            0, 4, 7, 0, 7, 3,
            5, 1, 2, 5, 2, 6,
            3, 7, 6, 3, 6, 2,
            0, 1, 5, 0, 5, 4
        ];

        var triangles = new MeshTriangle[faces.Length / 3];

        for (int i = 0; i < triangles.Length; i++)
        {
            triangles[i] = new MeshTriangle
            {
                V0 = corners[faces[i * 3]],
                V1 = corners[faces[i * 3 + 1]],
                V2 = corners[faces[i * 3 + 2]]
            };
        }

        return triangles;
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
        sphere.Material = new Material
        {
            Albedo = albedo,
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