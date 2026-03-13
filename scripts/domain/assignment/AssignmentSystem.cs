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

            // Operator arrived at the target's last-known position — hold there.
            case AssignmentPhase.PursuingTarget:
                StartHoldPosition(assignment, evt.Time);
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

        if (assignment.Phase != AssignmentPhase.PursuingTarget)
            return;

        var mov = assignment.CurrentMovement;
        if (mov == null || assignment.Character == null)
            return;

        // Repath from the operator's current position to the target's actual
        // position (the site entry point) so the operator closes on the right
        // spot instead of stopping at a stale repath destination.
        var closePath = DispatchNavPathfinder.FindPath(
            _dispatchNav.Graph,
            assignment.Character.Position.WorldPosition,
            evt.Character.Position.WorldPosition);

        if (closePath.IsValid)
        {
            mov.ReplacePath(closePath);
        }

        // Convert to static-path so MovementSystem stops repathing and the
        // movement self-arrives at the path end, triggering HoldingPosition.
        mov.ConvertToStaticPath();
    }

    // ── Tail: target exits a site ─────────────────────────────────────────────

    private void OnCharacterExitedSite(CharacterExitedSiteEvent evt)
    {
        // Only react to NPC target leaving a site.
        if (evt.Character.IsOperator)
            return;

        if (!_world.TryGetTailAssignmentForTarget(evt.Character.Id, out var assignment) || assignment == null)
            return;

        // Target started moving — operator pursues.
        if (assignment.Phase == AssignmentPhase.HoldingPosition ||
            assignment.Phase == AssignmentPhase.LostTarget)
        {
            StopCurrentOperation(assignment, evt.Time);
            StartPursuitPhase(assignment, evt.Character, evt.Time);
        }
        // Operator is still closing in on the site — abort and re-pursue.
        else if (assignment.Phase == AssignmentPhase.PursuingTarget)
        {
            StopCurrentMovement(assignment, evt.Time);
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

        // Always start with pursuit toward the target's current position.
        // If the target is stationary inside a site, pursuit will path to their
        // position and the operator will hold there once the path arrives.
        StartPursuitPhase(assignment, target, worldTime);
    }

    // ── Tail: hold at current nav position ───────────────────────────────────

    private void StartHoldPosition(Assignment assignment, double worldTime)
    {
        assignment.Phase = AssignmentPhase.HoldingPosition;

        var oldOperationId = assignment.CurrentOperation?.Id;

        // Operation with no SiteContext — the operator watches from their current
        // nav-graph position, not from a site entry point.
        var holdOperation = new Operation(
            id: Guid.NewGuid().ToString(),
            label: "Hold position",
            duration: double.MaxValue, // Indefinite — cancelled when target moves.
            visionType: OperationVisionType.Stakeout);

        if (assignment.Character != null)
            holdOperation.Participants.Add(assignment.Character);

        assignment.CurrentOperation = holdOperation;
        _world.UpdateAssignmentOperationIndex(assignment, oldOperationId);

        holdOperation.Start(worldTime);
        _eventBus.Publish(new OperationStartedEvent(holdOperation, worldTime));
    }

    // ── Tail: hold at last-known position (target lost) ──────────────────────

    private void StartLostTargetHold(Assignment assignment, double worldTime)
    {
        assignment.Phase = AssignmentPhase.LostTarget;

        var oldOperationId = assignment.CurrentOperation?.Id;

        var holdOperation = new Operation(
            id: Guid.NewGuid().ToString(),
            label: "Wait — target lost",
            duration: double.MaxValue,
            visionType: OperationVisionType.Stakeout);

        if (assignment.Character != null)
            holdOperation.Participants.Add(assignment.Character);

        assignment.CurrentOperation = holdOperation;
        _world.UpdateAssignmentOperationIndex(assignment, oldOperationId);

        holdOperation.Start(worldTime);
        _eventBus.Publish(new OperationStartedEvent(holdOperation, worldTime));
    }

    // ── Tail: start pursuit movement ─────────────────────────────────────────

    private void StartPursuitPhase(Assignment assignment, Character target, double worldTime)
    {
        assignment.Phase = AssignmentPhase.PursuingTarget;

        if (assignment.Character == null)
        {
            FailAssignment(assignment, worldTime);
            return;
        }

        var operatorPos = assignment.Character.Position.WorldPosition;
        var targetPos = target.Position.WorldPosition;

        var path = DispatchNavPathfinder.FindPath(
            _dispatchNav.Graph,
            operatorPos,
            targetPos);

        if (!path.IsValid)
        {
            GD.PushWarning($"[AssignmentSystem] Pursuit path invalid for assignment {assignment.Id}. Holding at last-known position.");
            StartLostTargetHold(assignment, worldTime);
            return;
        }

        var pursuitMovement = new Movement(
            id: Guid.NewGuid().ToString(),
            character: assignment.Character,
            origin: assignment.Character.CurrentSite,
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
            // Stakeout: character watches from the entry position outside the site.
            // They do not become an occupant and the operation is not registered on
            // the site's ActiveOperations list — it lives only in the assignment.
            if (assignment.Kind != AssignmentKind.StakeoutSite)
            {
                if (assignment.Character != null)
                    operation.SiteContext.AddOccupant(assignment.Character);

                operation.SiteContext.AddActiveOperation(operation);
            }
        }

        operation.Start(worldTime);
        _eventBus.Publish(new OperationStartedEvent(operation, worldTime));
    }

    private void StartReturnMovement(Assignment assignment, double worldTime)
    {
        if (assignment.BaseWorldPosition == null || assignment.Character == null)
        {
            assignment.Phase = AssignmentPhase.Completed;
            _eventBus.Publish(new AssignmentCompletedEvent(assignment, worldTime));
            return;
        }

        // Use the character's authoritative position as the return start.
        // This works for both site-based operations (character is at entry) and
        // position-based ones (stakeout / hold) where there is no SiteContext.
        var returnPath = DispatchNavPathfinder.FindPath(
            _dispatchNav.Graph,
            assignment.Character.Position.WorldPosition,
            assignment.BaseWorldPosition.Value);

        if (!returnPath.IsValid)
        {
            GD.PushWarning($"[AssignmentSystem] Return path for assignment {assignment.Id} is invalid.");
            assignment.Phase = AssignmentPhase.Completed;
            _eventBus.Publish(new AssignmentCompletedEvent(assignment, worldTime));
            return;
        }

        var returnMovement = new Movement(
            id: Guid.NewGuid().ToString(),
            character: assignment.Character,
            origin: assignment.Character.CurrentSite,
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

