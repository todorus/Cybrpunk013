using Godot;
using SurveillanceStategodot.scripts.domain.movement;

namespace SurveillanceStategodot.scripts.presentation.movement;

public partial class MovementVisual : Node3D
{
    private Movement? _movement;

    public void SetMovement(Movement movement)
    {
        if (_movement == movement)
            return;

        UnsubscribeFromMovement();

        _movement = movement;
        SubscribeToMovement();

        SyncImmediate();
    }

    public override void _ExitTree()
    {
        UnsubscribeFromMovement();
        base._ExitTree();
    }

    private void SubscribeToMovement()
    {
        if (_movement == null)
            return;

        _movement.PositionChanged += OnMovementPositionChanged;
        _movement.Arrived += OnMovementArrived;
    }

    private void UnsubscribeFromMovement()
    {
        if (_movement == null)
            return;

        _movement.PositionChanged -= OnMovementPositionChanged;
        _movement.Arrived -= OnMovementArrived;
    }

    private void OnMovementPositionChanged(Movement movement)
    {
        GlobalPosition = movement.CurrentWorldPosition;

        if (movement.CurrentForward.LengthSquared() > 0.0001f)
            LookAt(movement.CurrentWorldPosition + movement.CurrentForward, Vector3.Up, true);
    }

    private void OnMovementArrived(Movement movement)
    {
        QueueFree();
    }

    private void SyncImmediate()
    {
        if (_movement == null)
            return;

        GlobalPosition = _movement.CurrentWorldPosition;

        if (_movement.CurrentForward.LengthSquared() > 0.0001f)
            LookAt(_movement.CurrentWorldPosition + _movement.CurrentForward, Vector3.Up, true);
    }
}