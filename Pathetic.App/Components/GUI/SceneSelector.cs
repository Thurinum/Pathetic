using ImGuiNET;
using Pathetic.App.Scenes;
using Pathetic.SceneGraph.Components;
using Pathetic.SceneGraph.Scenes;

namespace Pathetic.App.Components.GUI;

public sealed class SceneSelector(SceneFactory sceneFactory, SceneManager sceneManager) : Component
{
    private const string WindowTitle = "Scene";
    private const int InitialSceneIndex = 0;

    private IReadOnlyList<SceneObject> _content = [];
    private int _selected = -1;

    protected override void Setup() => Load(InitialSceneIndex);

    protected override void GUI()
    {
        IReadOnlyList<ISceneDefinition> definitions = sceneFactory.Definitions;
        if (!ImGui.Begin(WindowTitle))
        {
            ImGui.End();
            return;
        }

        if (ImGui.BeginCombo("##scene", NameOf(definitions[_selected])))
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                if (ImGui.Selectable(NameOf(definitions[i]), i == _selected) && i != _selected)
                    Load(i);
            }

            ImGui.EndCombo();
        }

        ImGui.End();
    }

    private void Load(int index)
    {
        foreach (SceneObject obj in _content)
            sceneManager.DestroyLater(Parent.Owner, obj);

        _selected = index;
        _content = sceneFactory.Populate(Parent.Owner, sceneFactory.Definitions[index]);
    }

    private static string NameOf(ISceneDefinition definition) => definition.GetType().Name.Replace("Scene", string.Empty);
}
