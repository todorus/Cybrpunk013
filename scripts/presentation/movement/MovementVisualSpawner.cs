using Godot;
using SurveillanceStategodot.scripts.domain.movement;

namespace SurveillanceStategodot.scripts.presentation.movement;

public partial class MovementVisualSpawner : Node
{
    [Export] private PackedScene _movementVisualScene = null!;
    [Export] private Node3D _visualRoot = null!;
    
    private float _operatorVisionRange = 3f;
    
    private void SetOperatorVisionRange(float range)
    {
        _operatorVisionRange = range;
    }

    public MovementVisual SpawnForMovement(Movement movement)
    {
        var visual = _movementVisualScene.Instantiate<MovementVisual>();
        _visualRoot.AddChild(visual);
        visual.SetMovement(movement);
        visual.SetVisionRange(_operatorVisionRange);
        return visual;
    }
}