namespace Pathetic.App;

// Shared editor state: which scene object (by SceneObject.Id) is currently selected, 0 for none.
public sealed class SelectionState
{
    public uint SelectedObjectId { get; set; }
}
