using System;
using Godot;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.domain.system;
using SurveillanceStategodot.scripts.navigation.authoring;
using SurveillanceStategodot.scripts.navigation.query;

namespace SurveillanceStategodot.scripts.domain.assignment;

public sealed class AssignmentSystem : ISimulationSystem
{
    private readonly DispatchNav _dispatchNav;

    private WorldState _world = null!;
    private SimulationEventBus _eventBus = null!;

    public AssignmentSystem(DispatchNav dispatchNav)
    {
        _dispatchNav = dispatchNav;
    }

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
        assignment.Phase = AssignmentPhase.OutboundMovement;

        if (assignment.CurrentMovement != null)
        {
            _eventBus.Publish(new MovementStartedEvent(assignment.CurrentMovement, evt.Time));
        }
        else
        {
            StartOperationForAssignment(assignment, evt.Time);
        }
    }

    private void OnMovementArrived(MovementArrivedEvent evt)
    {
        if (!_world.TryGetAssignmentByMovementId(evt.Movement.Id, out var assignment) || assignment == null)
            return;

        if (assignment.Phase == AssignmentPhase.OutboundMovement)
        {
            StartOperationForAssignment(assignment, evt.Time);
            return;
        }

        if (assignment.Phase == AssignmentPhase.ReturnMovement)
        {
            assignment.CurrentMovement = null;
            assignment.Phase = AssignmentPhase.Completed;
            _eventBus.Publish(new AssignmentCompletedEvent(assignment, evt.Time));
        }
    }

    private void OnOperationCompleted(OperationCompletedEvent evt)
    {
        if (!_world.TryGetAssignmentByOperationId(evt.Operation.Id, out var assignment))
            return;

        switch (assignment.CompletionBehavior)
        {
            case AssignmentCompletionBehavior.ReturnToBase:
                StartReturnMovement(assignment, evt.Time);
                break;

            case AssignmentCompletionBehavior.AwaitSchedule:
            case AssignmentCompletionBehavior.None:
            default:
                assignment.Phase = AssignmentPhase.Completed;
                _eventBus.Publish(new AssignmentCompletedEvent(assignment, evt.Time));
                break;
        }
    }

    private void StartOperationForAssignment(Assignment assignment, double worldTime)
    {
        assignment.Phase = AssignmentPhase.OnSiteOperation;
        assignment.CurrentMovement = null;

        var operation = assignment.Operation;

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

        operation.Start(worldTime);
        _eventBus.Publish(new OperationStartedEvent(operation, worldTime));
    }

    private void StartReturnMovement(Assignment assignment, double worldTime)
    {
        if (assignment.BaseWorldPosition == null)
        {
            assignment.Phase = AssignmentPhase.Completed;
            _eventBus.Publish(new AssignmentCompletedEvent(assignment, worldTime));
            return;
        }

        var operationSite = assignment.Operation.SiteContext;
        if (operationSite == null)
        {
            assignment.Phase = AssignmentPhase.Completed;
            _eventBus.Publish(new AssignmentCompletedEvent(assignment, worldTime));
            return;
        }

        var startAnchor = DispatchNavQueries.GetClosestPointOnGraph(
            _dispatchNav.Graph,
            operationSite.GlobalPosition);

        var returnPath = DispatchNavPathfinder.FindPath(
            _dispatchNav.Graph,
            startAnchor,
            assignment.BaseWorldPosition.Value);

        if (!returnPath.IsValid)
        {
            GD.PushWarning($"Return path for assignment {assignment.Id} is invalid.");
            assignment.Phase = AssignmentPhase.Completed;
            _eventBus.Publish(new AssignmentCompletedEvent(assignment, worldTime));
            return;
        }

        var returnMovement = new Movement(
            id: Guid.NewGuid().ToString(),
            character: assignment.Character,
            origin: operationSite,
            destination: null,
            path: returnPath,
            initialPosition: returnPath.StartPosition);

        assignment.CurrentMovement = returnMovement;
        assignment.Phase = AssignmentPhase.ReturnMovement;

        _eventBus.Publish(new MovementStartedEvent(returnMovement, worldTime));
    }
}