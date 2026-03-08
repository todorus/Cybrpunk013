using Godot;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.interaction;

namespace SurveillanceStategodot.scripts.presentation.movement;

public partial class MovementPresenter : Node
{
    [Export] private MovementVisualSpawner _spawner = null!;

    [Export] private SimulationController _simulationController = null!;

    public override void _Ready()
    {
        _simulationController.EventBus.Subscribe<MovementStartedEvent>(OnMovementStarted);
    }

    public override void _ExitTree()
    {
        if (_simulationController?.EventBus != null)
        {
            _simulationController.EventBus.Unsubscribe<MovementStartedEvent>(OnMovementStarted);
        }
    }

    private void OnMovementStarted(MovementStartedEvent evt)
    {
        _spawner.SpawnForMovement(evt.Movement);
    }
}