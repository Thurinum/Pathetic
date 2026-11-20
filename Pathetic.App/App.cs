using Microsoft.Extensions.Options;
using Pathetic.App.Components.GUI;
using Pathetic.App.Scenes;
using Pathetic.Profiling;
using Pathetic.SceneGraph;
using Pathetic.SceneGraph.Scenes;

namespace Pathetic.App;

public class App(
    SceneLoop loop,
    SceneManager sceneManager,
    SceneFactory sceneFactory,
    IOptions<ProfilerOptions> profilerOptions)
{
    public void Run()
    {
        var scene = sceneFactory.Create();

        sceneManager.CreateObject(scene, "fpsCounter")
            .AddComponent<FPSCounter>();

        sceneManager.CreateObject(scene, "youssefAimePasTemporalAccumFix")
            .AddComponent<RenderSettingsComponent>();

        if (profilerOptions.Value.Enabled)
        {
            sceneManager.CreateObject(scene, nameof(ProfilerOverlay))
                .AddComponent<ProfilerOverlay>();
        }

        loop.Run(scene);
    }
}
