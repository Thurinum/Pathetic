using Microsoft.Extensions.Options;
using Pathetic.App.Components.GUI;
using Pathetic.Profiling;
using Pathetic.SceneGraph;
using Pathetic.SceneGraph.Scenes;

namespace Pathetic.App;

public sealed class App(
    SceneLoop loop,
    SceneManager sceneManager,
    IOptions<ProfilerOptions> profilerOptions)
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

        if (profilerOptions.Value.Enabled)
        {
            sceneManager.CreateObject(scene, nameof(ProfilerOverlay))
                .AddComponent<ProfilerOverlay>();
        }

        loop.Run(scene);
    }
}
