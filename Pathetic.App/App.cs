using System.Numerics;
using NeoVeldrid;
using Pathetic.App.Components;
using Pathetic.App.Components.Camera;
using Pathetic.App.Components.Editor;
using Pathetic.App.Components.GUI;
using Pathetic.Physics;
using Pathetic.Renderer.Utils;
using Pathetic.SceneGraph;

namespace Pathetic.App;

public class App(SceneLoop loop, SceneManager sceneManager)
{
    public void Run()
    {
        var scene = sceneManager.CreateScene();

        SceneManager.CreateObject(scene, "fpsCounter")
            .AddComponent<FPSCounter>();

        SceneManager.CreateObject(scene, "youssefAimePasTemporalAccumFix")
            .AddComponent<RenderSettingsComponent>();

        SceneManager.CreateObject(scene, "physics")
            .AddComponent<PhysicsSystem>();

        SceneManager.CreateObject(scene, "moveGizmo")
            .AddComponent<MoveGizmoComponent>();

        SceneManager.CreateObject(scene, "sphereSpawner")
            .AddComponent<SphereSpawner>();

        SceneFactory.AddSphere(scene, new Vector3(2.0f, -100.5f, 0.0f), 100.0f, Rgb(0.6f, 0.6f, 0.6f), dynamic: false);
        SceneFactory.AddSphere(scene, new Vector3(2.0f, 0.2f, 0.0f), 0.7f, Rgb(0.9f, 0.6f, 0.2f));
        SceneFactory.AddSphere(scene, new Vector3(1.3f, -0.3f, 0.9f), 0.2f, Rgb(0.2f, 0.6f, 0.9f));
        SceneFactory.AddSphere(scene, new Vector3(-0.2f, 1.0f, 0.0f), 0.7f, Rgb(0.0f, 0.0f, 0.0f), new RgbaByte(15, 12, 9, 255));
        
        var camera = SceneManager.CreateObject(scene, "camera");
        camera.AddComponent<CameraComponent>();
        var t = camera.GetComponent<Transform>();
        t.Position = new Vector3(0, 5, 10);

        for (int i = 0; i < 100; i++)
        {
            AddCircle(scene, Random.Shared.NextSingle() * (150 - 50), Random.Shared.NextSingle(), Random.Shared.NextSingle());
        }

        var orbit = camera.AddComponent<CameraControls>();
        orbit.Target = new Vector3(2,0,0);
        
        AddPlane(scene, new Vector3( 0, -1,  0), Quaternion.FromEuler(new Vector3(-90,   0,  0))); // floor  +Y
        AddPlane(scene, new Vector3( 0,  1,  0), Quaternion.FromEuler(new Vector3( 90,   0,  0))); // ceil   -Y
        AddPlane(scene, new Vector3(-1,  0,  0), Quaternion.FromEuler(new Vector3(  0,  90,  0))); // left   +X
        AddPlane(scene, new Vector3( 1,  0,  0), Quaternion.FromEuler(new Vector3(  0, -90,  0))); // right  -X
        AddPlane(scene, new Vector3( 0,  0, -1), Quaternion.FromEuler(new Vector3(  0,   0,  0))); // back   +Z
        
        loop.Run(scene);
    }
    
    private static void AddPlane(Scene scene, Vector3 pos, Quaternion rot)
    {
        var obj = SceneManager.CreateObject(scene, "plane");
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

    private static void AddCircle(Scene scene, float r, float x, float y)
    {
        var obj = SceneManager.CreateObject(scene, "circle");

        obj.Transform.Position = new Vector3(x, y, 0.0f);
        
        var circle = obj.AddComponent<CircleComponent>();
        circle.Radius = r;
        
        var mover = obj.AddComponent<RandomMover>();
        mover.Speed = 0.2f;
    }
}