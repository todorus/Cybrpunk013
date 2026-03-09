using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.assignment;

public sealed class AssignmentSystem : ISimulationSystem
{
    private WorldState _world = null!;
    private SimulationEventBus _eventBus = null!;

    public void Initialize(WorldState world, SimulationEventBus eventBus)
    {
        _world = world;
        _eventBus = eventBus;

        _eventBus.Subscribe<AssignmentCreatedEvent>(OnAssignmentCreated);
        _eventBus.Subscribe<MovementArrivedEvent>(OnMovementArrived);
        _eventBus.Subscribe<OperationCompletedEvent>(OnOperationCompleted);
    }

    public void Tick(double delta)
    {
    }

    private void OnAssignmentCreated(AssignmentCreatedEvent evt)
    {
        var assignment = evt.Assignment;

        _world.RegisterAssignment(assignment);
        assignment.State = AssignmentState.Moving;

        _eventBus.Publish(new MovementStartedEvent(assignment.Movement, evt.Time));
    }

    private void OnMovementArrived(MovementArrivedEvent evt)
    {
        if (!_world.TryGetAssignmentByMovementId(evt.Movement.Id, out var assignment))
            return;

        assignment.State = AssignmentState.Operating;

        var operation = assignment.Operation;
        operation.MovementContext = evt.Movement;
        operation.SiteContext ??= evt.Movement.Destination;

        if (assignment.Character != null && !operation.Participants.Contains(assignment.Character))
        {
            operation.Participants.Add(assignment.Character);
        }

        if (operation.SiteContext != null)
        {
            if (assignment.Character != null && !operation.SiteContext.Occupants.Contains(assignment.Character))
            {
                operation.SiteContext.Occupants.Add(assignment.Character);
            }

            if (!operation.SiteContext.ActiveOperations.Contains(operation))
            {
                operation.SiteContext.ActiveOperations.Add(operation);
            }
        }

        operation.Start(evt.Time);

        _eventBus.Publish(new OperationStartedEvent(operation, evt.Time));
    }

    private void OnOperationCompleted(OperationCompletedEvent evt)
    {
        if (!_world.TryGetAssignmentByOperationId(evt.Operation.Id, out var assignment))
            return;

        assignment.State = AssignmentState.Completed;
        _eventBus.Publish(new AssignmentCompletedEvent(assignment, evt.Time));
    }
}