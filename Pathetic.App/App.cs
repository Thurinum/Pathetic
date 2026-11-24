using Pathetic.App.Components.GUI;
using Pathetic.SceneGraph;
using Pathetic.SceneGraph.Scenes;

namespace Pathetic.App;

public sealed class App(
    SceneLoop loop,
    SceneManager sceneManager)
{
    public void Run()
    {
        var scene = sceneManager.CreateScene();

        sceneManager.CreateObject(scene, "fpsCounter")
            .AddComponent<FPSCounter>();

        sceneManager.CreateObject(scene, "youssefAimePasTemporalAccumFix")
            .AddComponent<RenderSettingsComponent>();

        sceneManager.CreateObject(scene, nameof(SceneSelector))
            .AddComponent<SceneSelector>();

#if PROFILING
        sceneManager.CreateObject(scene, nameof(ProfilerOverlay))
            .AddComponent<ProfilerOverlay>();
#endif

        loop.Run(scene);
    }
}
