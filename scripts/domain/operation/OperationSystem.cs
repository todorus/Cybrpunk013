using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.operation;

public sealed class OperationSystem : ISimulationSystem
{
    private WorldState _world = null!;
    private SimulationEventBus _eventBus = null!;

    private readonly List<Operation> _activeOperations = new();

    public void Initialize(WorldState world, SimulationEventBus eventBus)
    {
        _world = world;
        _eventBus = eventBus;

        _eventBus.Subscribe<OperationStartedEvent>(OnOperationStarted);
    }

    public void Tick(double delta)
    {
        for (int i = _activeOperations.Count - 1; i >= 0; i--)
        {
            var operation = _activeOperations[i];

            if (!operation.IsComplete(_world.Time))
                continue;

            operation.State = OperationState.Completed;
            operation.SiteContext?.ActiveOperations.Remove(operation);

            _activeOperations.RemoveAt(i);

            _eventBus.Publish(new OperationCompletedEvent(operation, _world.Time));
        }
    }

    private void OnOperationStarted(OperationStartedEvent evt)
    {
        if (!_activeOperations.Contains(evt.Operation))
        {
            _activeOperations.Add(evt.Operation);
        }
    }
}