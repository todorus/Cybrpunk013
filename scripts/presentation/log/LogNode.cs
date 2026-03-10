using Godot;
using SurveillanceStategodot.scripts.domain.observation;
using SurveillanceStategodot.scripts.interaction;

namespace SurveillanceStategodot.scripts.presentation.log;

public partial class LogNode : Container
{
    [Export]
    private SimulationController _simulationController;
    
    [Export]
    private PackedScene _LogEntryScene;
    
    public override void _Ready()
    {
        _simulationController.EventBus.Subscribe<ObservationCreatedEvent>(OnObservationEvent);
    }

    private void OnObservationEvent(ObservationCreatedEvent obj)
    {
        var logEntryNode = _LogEntryScene.Instantiate<LogEntryNode>();
        logEntryNode.WorldState = _simulationController.World;
        logEntryNode.Observation = obj.Observation;
        AddChild(logEntryNode);
    }
}