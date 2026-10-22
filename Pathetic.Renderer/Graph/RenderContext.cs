using NeoVeldrid;
using Pathetic.Renderer.Resources;

namespace Pathetic.Renderer.Graph;

public readonly record struct RenderContext
{
    public required CommandList Cmd { get; init; }
    public required Texture Target { get; init; }
    public required uint Width { get; init; }
    public required uint Height { get; init; }
    public required float DeltaTime { get; init; }
    public required uint FrameIndex { get; init; }
    public required IResourceMetadata Resources { get; init; }
}