namespace Pathetic.App.RenderPasses;

public sealed class RenderDebugSettings
{
    public uint Revision { get; private set; }
    public bool DisplayNormals { get; set => Set(ref field, value); }
    public bool SmoothShading { get; set => Set(ref field, value); }

    private void Set(ref bool field, bool value)
    {
        if (field == value)
            return;

        field = value;
        Revision++;
    }
}
