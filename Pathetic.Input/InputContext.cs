namespace Pathetic.Input;

public sealed class InputContext
{
    public required string Name { get; init; }
    public bool Enabled { get; set; }
    public required List<InputAction> Actions { get; init; } = [];
}