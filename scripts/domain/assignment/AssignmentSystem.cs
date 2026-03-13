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
        _eventBus.Subscribe<CharacterEnteredSiteEvent>(OnCharacterEnteredSite);
        _eventBus.Subscribe<CharacterExitedSiteEvent>(OnCharacterExitedSite);
    }

    public void Tick(double delta)
    {
    }

    // ── Assignment created ────────────────────────────────────────────────────

    private void OnAssignmentCreated(AssignmentCreatedEvent evt)
    {
        var assignment = evt.Assignment;

        _world.RegisterAssignment(assignment);

        if (assignment.Kind == AssignmentKind.TailCharacter)
        {
            StartTailAssignment(assignment, evt.Time);
            return;
        }

        // Normal VisitSite / StakeoutSite flow.
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

    // ── Movement arrived ──────────────────────────────────────────────────────

    private void OnMovementArrived(MovementArrivedEvent evt)
    {
        if (!_world.TryGetAssignmentByMovementId(evt.Movement.Id, out var assignment) || assignment == null)
            return;

        // Clear current movement reference regardless of phase.
        assignment.CurrentMovement = null;

        switch (assignment.Phase)
        {
            case AssignmentPhase.OutboundMovement:
                StartOperationForAssignment(assignment, evt.Time);
                break;

            case AssignmentPhase.ReturnMovement:
                assignment.Phase = AssignmentPhase.Completed;
                _eventBus.Publish(new AssignmentCompletedEvent(assignment, evt.Time));
                break;

            // Pursuit movements for tail assignments are force-arrived externally;
            // the phase transition is handled by OnCharacterEnteredSite instead.
            case AssignmentPhase.PursuingTarget:
                // Do nothing here — OnCharacterEnteredSite drives the transition.
                break;
        }
    }

    // ── Operation completed ───────────────────────────────────────────────────

    private void OnOperationCompleted(OperationCompletedEvent evt)
    {
        if (!_world.TryGetAssignmentByOperationId(evt.Operation.Id, out var assignment))
            return;

        if (assignment.Kind == AssignmentKind.TailCharacter)
        {
            // Tail watch operations can complete if manually ended; handled elsewhere.
            // If they naturally time out (shouldn't happen for tails), just leave the phase.
            return;
        }

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

    // ── Tail: target enters a site ────────────────────────────────────────────

    private void OnCharacterEnteredSite(CharacterEnteredSiteEvent evt)
    {
        // Only react to NPC target entering a site.
        if (evt.Character.IsOperator)
            return;

        if (!_world.TryGetTailAssignmentForTarget(evt.Character.Id, out var assignment) || assignment == null)
            return;

        if (assignment.Phase == AssignmentPhase.PursuingTarget)
        {
            // Stop pursuit movement.
            StopCurrentMovement(assignment, evt.Time);

            // Switch to watching the new site.
            StartWatchSitePhase(assignment, evt.Site, evt.Time);
        }
    }

    // ── Tail: target exits a site ─────────────────────────────────────────────

    private void OnCharacterExitedSite(CharacterExitedSiteEvent evt)
    {
        // Only react to NPC target leaving a site.
        if (evt.Character.IsOperator)
            return;

        if (!_world.TryGetTailAssignmentForTarget(evt.Character.Id, out var assignment) || assignment == null)
            return;

        if (assignment.Phase == AssignmentPhase.WatchingTargetSite)
        {
            // Stop current watch operation.
            StopCurrentOperation(assignment, evt.Time);

            // Switch to pursuit.
            StartPursuitPhase(assignment, evt.Character, evt.Time);
        }
    }

    // ── Tail assignment bootstrap ─────────────────────────────────────────────

    private void StartTailAssignment(Assignment assignment, double worldTime)
    {
        var target = assignment.TargetCharacter;
        if (target == null)
        {
            FailAssignment(assignment, worldTime);
            return;
        }

        // If target is at a site right now, start watching.
        if (target.CurrentSite != null)
        {
            StartWatchSitePhase(assignment, target.CurrentSite, worldTime);
        }
        else if (target.CurrentMovement != null)
        {
            // Target is already moving — start pursuit immediately.
            StartPursuitPhase(assignment, target, worldTime);
        }
        else
        {
            // Unknown position — fail for now.
            FailAssignment(assignment, worldTime);
        }
    }

    // ── Tail: start watching a site ──────────────────────────────────────────

    private void StartWatchSitePhase(Assignment assignment, Site site, double worldTime)
    {
        assignment.Phase = AssignmentPhase.WatchingTargetSite;

        var oldOperationId = assignment.CurrentOperation?.Id;

        var watchOperation = new Operation(
            id: Guid.NewGuid().ToString(),
            label: $"Watch {site.Label}",
            duration: double.MaxValue, // Indefinite — cancelled externally.
            visionType: OperationVisionType.Stakeout)
        {
            SiteContext = site
        };

        if (assignment.Character != null)
            watchOperation.Participants.Add(assignment.Character);

        assignment.CurrentOperation = watchOperation;
        _world.UpdateAssignmentOperationIndex(assignment, oldOperationId);

        site.AddActiveOperation(watchOperation);
        watchOperation.Start(worldTime);

        _eventBus.Publish(new OperationStartedEvent(watchOperation, worldTime));
    }

    // ── Tail: start pursuit movement ─────────────────────────────────────────

    private void StartPursuitPhase(Assignment assignment, Character target, double worldTime)
    {
        assignment.Phase = AssignmentPhase.PursuingTarget;

        // Determine operator's current world position from the authoritative position component.
        Vector3 operatorPos;
        if (assignment.Character != null)
            operatorPos = assignment.Character.Position.WorldPosition;
        else
        {
            FailAssignment(assignment, worldTime);
            return;
        }

        // Determine target's current world position.
        Vector3 targetPos;
        if (target.CurrentSite != null)
            targetPos = target.CurrentSite.EntryPosition;
        else
            targetPos = target.Position.WorldPosition;

        var path = DispatchNavPathfinder.FindPath(
            _dispatchNav.Graph,
            operatorPos,
            targetPos);

        if (!path.IsValid)
        {
            GD.PushWarning($"[AssignmentSystem] Tail pursuit path invalid for assignment {assignment.Id}.");
            assignment.Phase = AssignmentPhase.LostTarget;
            return;
        }

        var pursuitMovement = new Movement(
            id: Guid.NewGuid().ToString(),
            character: assignment.Character,
            origin: assignment.Character?.CurrentSite,
            targetCharacter: target,
            initialPath: path);

        assignment.CurrentMovement = pursuitMovement;
        _eventBus.Publish(new MovementStartedEvent(pursuitMovement, worldTime));
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    private void StopCurrentOperation(Assignment assignment, double worldTime)
    {
        var op = assignment.CurrentOperation;
        if (op == null || op.State != OperationState.Active)
            return;

        op.State = OperationState.Completed;
        op.SiteContext?.RemoveActiveOperation(op);

        var oldId = op.Id;
        assignment.CurrentOperation = null;
        _world.UpdateAssignmentOperationIndex(assignment, oldId);

        _eventBus.Publish(new OperationCompletedEvent(op, worldTime));
    }

    private void StopCurrentMovement(Assignment assignment, double worldTime)
    {
        var mov = assignment.CurrentMovement;
        if (mov == null) return;

        // Clear from assignment first so MovementSystem's OnAssignmentCancelled
        // (if triggered) doesn't double-process it.
        assignment.CurrentMovement = null;

        // ForceArrive will fire Arrived event; OnMovementArrived checks PursuingTarget phase
        // and does nothing, so this is safe.
        mov.ForceArrive();
    }

    private void StartOperationForAssignment(Assignment assignment, double worldTime)
    {
        assignment.Phase = AssignmentPhase.OnSiteOperation;
        assignment.CurrentMovement = null;

        var operation = assignment.CurrentOperation;
        if (operation == null)
        {
            assignment.Phase = AssignmentPhase.Completed;
            _eventBus.Publish(new AssignmentCompletedEvent(assignment, worldTime));
            return;
        }

        if (assignment.Character != null && !operation.Participants.Contains(assignment.Character))
        {
            operation.Participants.Add(assignment.Character);
        }

        if (operation.SiteContext != null)
        {
            if (assignment.Character != null)
            {
                operation.SiteContext.AddOccupant(assignment.Character);
            }

            operation.SiteContext.AddActiveOperation(operation);
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

        var operationSite = assignment.CurrentOperation?.SiteContext;
        if (operationSite == null)
        {
            assignment.Phase = AssignmentPhase.Completed;
            _eventBus.Publish(new AssignmentCompletedEvent(assignment, worldTime));
            return;
        }

        DispatchNavEdgeAnchor startAnchor;
        if (operationSite.NavAnchor.HasValue)
        {
            startAnchor = operationSite.NavAnchor.Value;
        }
        else
        {
            startAnchor = DispatchNavQueries.GetClosestPointOnGraph(
                _dispatchNav.Graph,
                operationSite.GlobalPosition);
        }

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
            path: returnPath);

        assignment.CurrentMovement = returnMovement;
        assignment.Phase = AssignmentPhase.ReturnMovement;

        _eventBus.Publish(new MovementStartedEvent(returnMovement, worldTime));
    }

    private void FailAssignment(Assignment assignment, double worldTime)
    {
        assignment.Phase = AssignmentPhase.Failed;
        GD.PushWarning($"[AssignmentSystem] Assignment {assignment.Id} failed.");
        _eventBus.Publish(new AssignmentCompletedEvent(assignment, worldTime));
    }
}

