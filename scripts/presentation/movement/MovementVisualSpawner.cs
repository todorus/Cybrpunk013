using Godot;
using SurveillanceStategodot.scripts.domain.movement;

namespace SurveillanceStategodot.scripts.presentation.movement;

public partial class MovementVisualSpawner : Node
{
    [Export] private PackedScene _movementVisualScene = null!;
    [Export] private Node3D _visualRoot = null!;

    public MovementVisual SpawnForMovement(Movement movement)
    {
        var visual = _movementVisualScene.Instantiate<MovementVisual>();
        _visualRoot.AddChild(visual);
        visual.SetMovement(movement);
        return visual;
    }
}