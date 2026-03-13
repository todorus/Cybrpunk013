# Domain Layer – Source Export

_Generated: 2026-03-13_

---

## `scripts/domain/Character.cs`

```csharp
using System.Collections.Generic;
using Godot;
using SurveillanceStategodot.scripts.domain.assignment;
using SurveillanceStategodot.scripts.domain.communication;
using SurveillanceStategodot.scripts.domain.interrupt;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.domain.schedule;

namespace SurveillanceStategodot.scripts.domain;

public sealed class Character
{
    public string Id { get; }
    public string DisplayName { get; set; }
    public bool IsOperator { get; set; }

    /// <summary>
    /// Authoritative location kind for this character.
    /// Set by the system that owns each transition; never mutated directly by observers.
    /// </summary>
    public CharacterLocationType LocationType { get; set; } = CharacterLocationType.Base;

    public SuspicionLevel SuspicionLevel { get; set; } = SuspicionLevel.None;

    /// <summary>
    /// World-units per second. Defaults to MovementSystem.DefaultSpeed.
    /// Designers can override this per character via CharacterResource.
    /// </summary>
    public float MovementSpeed { get; set; } = MovementSystem.DefaultSpeed;

    /// <summary>
    /// Vision radius in world-units. Defaults to VisionSystem.DefaultVisionRange.
    /// Designers can override this per character via CharacterResource.
    /// </summary>
    public float VisionRange { get; set; }

    public Schedule? Schedule { get; set; }
    public Site? CurrentSite { get; set; }
    public Movement? CurrentMovement { get; set; }

    /// <summary>
    /// Authoritative world-space position of the character.
    /// Updated by MovementSystem on each tick and set explicitly when spawning.
    /// </summary>
    public CharacterPosition Position { get; } = new(Vector3.Zero);

    /// <summary>The currently active interrupt, if any.</summary>
    public CharacterInterrupt? ActiveInterrupt { get; set; }

    /// <summary>
    /// A baseline schedule assignment that was suspended by an interrupt with Suspend disposition.
    /// Restored by InterruptSystem when the interrupt clears.
    /// </summary>
    public Assignment? SuspendedAssignment { get; set; }

    public List<Interceptor> Interceptors { get; } = new();

    public Character(string id, string displayName, bool isOperator = false)
    {
        Id = id;
        DisplayName = displayName;
        IsOperator = isOperator;
    }
}
```

## `scripts/domain/CharacterLocationChangedEvent.cs`

```csharp
using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain;

public sealed record CharacterLocationChangedEvent(
    Character Character,
    CharacterLocationType PreviousLocation,
    CharacterLocationType NewLocation,
    double Time) : IDomainEvent;


```

## `scripts/domain/CharacterLocationType.cs`

```csharp
namespace SurveillanceStategodot.scripts.domain;

public enum CharacterLocationType
{
    Base,     // Off-map, at HQ / home
    NavGraph, // On the map, moving or holding position
    Site      // Inside a site building
}


```

## `scripts/domain/CharacterPosition.cs`

```csharp
using System;
using Godot;

namespace SurveillanceStategodot.scripts.domain;

/// <summary>
/// Authoritative world-space position component for a Character.
/// Owned by the Character; updated by MovementSystem on each tick and set
/// explicitly when spawning a character at a known location.
/// Consumers (VisionSystem, AssignmentSystem, etc.) read from here rather than
/// from Movement.CurrentWorldPosition.
/// </summary>
public sealed class CharacterPosition
{
    public Vector3 WorldPosition { get; private set; }
    public Vector3 Forward { get; private set; } = Vector3.Forward;

    public event Action<CharacterPosition>? Changed;

    public CharacterPosition(Vector3 initialPosition)
    {
        WorldPosition = initialPosition;
    }

    /// <summary>Sets position and forward direction (called every movement tick).</summary>
    public void Update(Vector3 position, Vector3 forward)
    {
        WorldPosition = position;
        if (forward.LengthSquared() > 0.0001f)
            Forward = forward.Normalized();
        Changed?.Invoke(this);
    }

    /// <summary>Sets position only, keeping forward unchanged (used when spawning).</summary>
    public void Set(Vector3 position)
    {
        WorldPosition = position;
        Changed?.Invoke(this);
    }
}


```

## `scripts/domain/SiteAsset.cs`

```csharp
namespace SurveillanceStategodot.scripts.domain;

public class SiteAsset
{
    
}
```

## `scripts/domain/SiteVisibility.cs`

```csharp
public enum SiteVisibility
{
    Hidden,
    Suspected,
    Known,
    Confirmed
}
```

## `scripts/domain/SuspicionLevel.cs`

```csharp
public enum SuspicionLevel
{
    None,
    Uneasy,
    Suspicious,
    Alert,
    Paranoid
}
```

## `scripts/domain/assignment/Assignment.cs`

```csharp
using Godot;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.domain.assignment;

public sealed class Assignment
{
    public string Id { get; }
    public AssignmentKind Kind { get; }
    public Character? Character { get; }

    /// <summary>
    /// For TailCharacter assignments: the NPC being tailed.
    /// </summary>
    public Character? TargetCharacter { get; }

    /// <summary>
    /// The currently active operation for this assignment.
    /// May change over the lifetime of a TailCharacter assignment (watch → pursue → watch …).
    /// Null during movement-only phases.
    /// </summary>
    public Operation? CurrentOperation { get; set; }

    public Movement? CurrentMovement { get; set; }

    public AssignmentCompletionBehavior CompletionBehavior { get; set; } = AssignmentCompletionBehavior.None;
    public AssignmentPhase Phase { get; set; } = AssignmentPhase.Planned;
    public AssignmentSource Source { get; set; } = AssignmentSource.PlayerOrder;

    // Set when Source == Interrupt; links back to the originating CharacterInterrupt.
    public string? InterruptId { get; set; }

    // Optional explicit home/base destination for return logic.
    public Vector3? BaseWorldPosition { get; set; }

    /// <summary>
    /// Standard VisitSite / StakeoutSite assignment constructor.
    /// </summary>
    public Assignment(
        string id,
        Character? character,
        Operation operation,
        Movement? currentMovement,
        AssignmentKind kind = AssignmentKind.VisitSite)
    {
        Id = id;
        Kind = kind;
        Character = character;
        CurrentOperation = operation;
        CurrentMovement = currentMovement;
    }

    /// <summary>
    /// TailCharacter assignment constructor — no initial operation or movement;
    /// AssignmentSystem drives what is created based on target state.
    /// </summary>
    public Assignment(
        string id,
        Character? character,
        Character targetCharacter)
    {
        Id = id;
        Kind = AssignmentKind.TailCharacter;
        Character = character;
        TargetCharacter = targetCharacter;
        CurrentOperation = null;
        CurrentMovement = null;
    }
}


```

## `scripts/domain/assignment/AssignmentCompletionBehavior.cs`

```csharp
namespace SurveillanceStategodot.scripts.domain.assignment;

public enum AssignmentCompletionBehavior
{
    None,
    ReturnToBase,
    AwaitSchedule
}
```

## `scripts/domain/assignment/AssignmentEvents.cs`

```csharp
using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.assignment;

public sealed record AssignmentCreatedEvent(Assignment Assignment, double Time) : IDomainEvent;
public sealed record AssignmentCompletedEvent(Assignment Assignment, double Time) : IDomainEvent;
public sealed record AssignmentCancelledEvent(Assignment Assignment, double Time) : IDomainEvent;
public sealed record AssignmentCancelRequestedEvent(Assignment Assignment, double Time) : IDomainEvent;

```

## `scripts/domain/assignment/AssignmentKind.cs`

```csharp
namespace SurveillanceStategodot.scripts.domain.assignment;

public enum AssignmentKind
{
    VisitSite,
    StakeoutSite,
    TailCharacter
}


```

## `scripts/domain/assignment/AssignmentPhase.cs`

```csharp
namespace SurveillanceStategodot.scripts.domain.assignment;

public enum AssignmentPhase
{
    Planned,
    OutboundMovement,
    OnSiteOperation,
    ReturnMovement,
    // Tail-specific phases
    HoldingPosition,  // Operator is stationary, watching the target's last known position.
    PursuingTarget,   // Operator is moving toward the target.
    LostTarget,       // Target lost — operator holds at last known position.
    // Terminal phases
    Completed,
    Cancelled,
    Failed
}
```

## `scripts/domain/assignment/AssignmentSource.cs`

```csharp
namespace SurveillanceStategodot.scripts.domain.assignment;

public enum AssignmentSource
{
    PlayerOrder,
    Schedule,
    Interrupt
}


```

## `scripts/domain/assignment/AssignmentSystem.cs`

```csharp
using System;
using Godot;
using SurveillanceStategodot.scripts.domain;
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
        _eventBus.Subscribe<CharacterLocationChangedEvent>(OnCharacterLocationChanged);
        _eventBus.Subscribe<AssignmentCancelRequestedEvent>(OnAssignmentCancelRequested);
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

        assignment.CurrentMovement = null;

        switch (assignment.Phase)
        {
            case AssignmentPhase.OutboundMovement:
                if (assignment.Kind == AssignmentKind.StakeoutSite)
                    StartStakeoutHold(assignment, evt.Time);
                else
                    StartOperationForAssignment(assignment, evt.Time);
                break;

            case AssignmentPhase.ReturnMovement:
                assignment.Phase = AssignmentPhase.Completed;
                if (assignment.Character != null)
                {
                    var prev = assignment.Character.LocationType;
                    assignment.Character.LocationType = CharacterLocationType.Base;
                    _eventBus.Publish(new CharacterLocationChangedEvent(
                        assignment.Character,
                        prev,
                        CharacterLocationType.Base,
                        evt.Time));
                }
                _eventBus.Publish(new AssignmentCompletedEvent(assignment, evt.Time));
                break;

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

    // ── Tail: target location changed ─────────────────────────────────────────

    private void OnCharacterLocationChanged(CharacterLocationChangedEvent evt)
    {
        // Only react to NPC targets.
        if (evt.Character.IsOperator) return;

        if (!_world.TryGetTailAssignmentForTarget(evt.Character.Id, out var assignment) || assignment == null)
            return;

        if (evt.NewLocation == CharacterLocationType.Site)
        {
            // Target entered a site — close in on the site entry point.
            if (assignment.Phase != AssignmentPhase.PursuingTarget)
                return;

            var mov = assignment.CurrentMovement;
            if (mov == null || assignment.Character == null)
                return;

            // Repath to the target's actual position (the site entry point) so the
            // operator closes on the right spot instead of stopping at a stale destination.
            var closePath = DispatchNavPathfinder.FindPath(
                _dispatchNav.Graph,
                assignment.Character.Position.WorldPosition,
                evt.Character.Position.WorldPosition);

            if (closePath.IsValid)
                mov.ReplacePath(closePath);

            // Convert to static-path so MovementSystem stops repathing and the
            // movement self-arrives at the path end, triggering HoldingPosition.
            mov.ConvertToStaticPath();
        }
        else if (evt.NewLocation == CharacterLocationType.NavGraph)
        {
            // Target started moving — operator pursues.
            if (assignment.Phase == AssignmentPhase.HoldingPosition ||
                assignment.Phase == AssignmentPhase.LostTarget)
            {
                StopCurrentOperation(assignment, evt.Time);
                StartPursuitPhase(assignment, evt.Character, evt.Time);
            }
            else if (assignment.Phase == AssignmentPhase.PursuingTarget)
            {
                // Operator is still closing in — abort current path and re-pursue.
                StopCurrentMovement(assignment, evt.Time);
                StartPursuitPhase(assignment, evt.Character, evt.Time);
            }
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
            operation.Participants.Add(assignment.Character);

        if (operation.SiteContext != null)
        {
            if (assignment.Character != null)
                operation.SiteContext.AddOccupant(assignment.Character);
            operation.SiteContext.AddActiveOperation(operation);
        }

        operation.Start(worldTime);
        _eventBus.Publish(new OperationStartedEvent(operation, worldTime));
    }

    // ── StakeoutSite specific ─────────────────────────────────────────────────

    /// <summary>
    /// Stakeout: operator arrived at the site entry position and now watches from
    /// the nav graph without entering the site. Uses the authored operation's
    /// duration, then returns to base via normal completion behavior.
    /// </summary>
    private void StartStakeoutHold(Assignment assignment, double worldTime)
    {
        assignment.Phase = AssignmentPhase.OnSiteOperation;

        // Use the authored operation for duration and label; replace it with a
        // position-only hold that does not touch any site state.
        var oldOperationId = assignment.CurrentOperation?.Id;
        var authored = assignment.CurrentOperation;

        var holdOperation = new Operation(
            id: Guid.NewGuid().ToString(),
            label: authored?.Label ?? "Stakeout",
            duration: authored?.Duration ?? 30.0,
            visionType: OperationVisionType.Stakeout);

        if (assignment.Character != null)
            holdOperation.Participants.Add(assignment.Character);

        // No SiteContext — the operator is on the street, not inside the site.
        assignment.CurrentOperation = holdOperation;
        _world.UpdateAssignmentOperationIndex(assignment, oldOperationId);

        holdOperation.Start(worldTime);
        _eventBus.Publish(new OperationStartedEvent(holdOperation, worldTime));
    }

    private void StartReturnMovement(Assignment assignment, double worldTime)
    {
        if (assignment.BaseWorldPosition == null || assignment.Character == null)
        {
            assignment.Phase = AssignmentPhase.Completed;
            _eventBus.Publish(new AssignmentCompletedEvent(assignment, worldTime));
            return;
        }

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

    // ── Player-requested recall ───────────────────────────────────────────────

    private void OnAssignmentCancelRequested(AssignmentCancelRequestedEvent evt)
    {
        var assignment = evt.Assignment;

        // Ignore if already in a terminal or return phase.
        if (assignment.Phase is AssignmentPhase.Completed
            or AssignmentPhase.Cancelled
            or AssignmentPhase.Failed
            or AssignmentPhase.ReturnMovement)
            return;

        StopCurrentOperation(assignment, evt.Time);
        StopCurrentMovement(assignment, evt.Time);
        StartReturnMovement(assignment, evt.Time);
    }
}


```

## `scripts/domain/assignment/Option.cs`

```csharp
using System;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.domain.assignment;

public class Option
{
    public string Id { get; }
    public string Label { get; }
    public double Duration { get; }
    public OperationVisionType VisionType { get; }
    public ComplianceType ComplianceType { get; }

    public Option(string id, string label, double duration, OperationVisionType visionType = OperationVisionType.None, ComplianceType complianceType = ComplianceType.Compliant)
    {
        Id = id;
        Label = label;
        Duration = duration;
        VisionType = visionType;
        ComplianceType = complianceType;
    }

    public Operation ToOperation(Movement movement, Site site)
    {
        return new Operation(
            id: Guid.NewGuid().ToString(),
            label: Label,
            duration: Duration,
            visionType: VisionType,
            complianceType: ComplianceType)
        {
            SiteContext = site,
            MovementContext = movement
        };
    }
}
```

## `scripts/domain/communication/Communication.cs`

```csharp
using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.domain.communication;

public sealed class Communication
{
    public string Id { get; }
    public CommunicationType Type { get; }
    public Character Sender { get; }
    public List<Character> Recipients { get; } = new();
    public Operation SourceOperation { get; }
    public Site? SourceSite { get; }
    public double Time { get; }
    public int EncryptionLevel { get; set; }
    public List<string> PayloadTags { get; } = new();

    public Communication(
        string id,
        CommunicationType type,
        Character sender,
        Operation sourceOperation,
        Site? sourceSite,
        double time)
    {
        Id = id;
        Type = type;
        Sender = sender;
        SourceOperation = sourceOperation;
        SourceSite = sourceSite;
        Time = time;
    }
}
```

## `scripts/domain/communication/CommunicationEvents.cs`

```csharp
using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.communication;

public sealed record CommunicationEmittedEvent(Communication Communication, double Time) : IDomainEvent;
public sealed record InterceptCreatedEvent(Intercept Intercept, double Time) : IDomainEvent;
```

## `scripts/domain/communication/CommunicationType.cs`

```csharp
namespace SurveillanceStategodot.scripts.domain.communication;

public enum CommunicationType
{
    PhoneCall,
    PhoneMessage,
    OnlineMessage,
    Speech,
    InPersonBriefing
}


```

## `scripts/domain/communication/Intercept.cs`

```csharp
namespace SurveillanceStategodot.scripts.domain.communication;

public sealed class Intercept
{
    public string Id { get; }
    public Communication Communication { get; }
    public Interceptor Interceptor { get; }
    public InterceptQuality Quality { get; set; }
    public double Time { get; }

    public Intercept(string id, Communication communication, Interceptor interceptor, double time)
    {
        Id = id;
        Communication = communication;
        Interceptor = interceptor;
        Time = time;
    }
}
```

## `scripts/domain/communication/InterceptQuality.cs`

```csharp
namespace SurveillanceStategodot.scripts.domain.communication;

public enum InterceptQuality
{
    MetadataOnly,
    PartialContent,
    FullContent
}


```

## `scripts/domain/communication/Interceptor.cs`

```csharp
using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.domain.communication;

public sealed class Interceptor
{
    public string Id { get; }
    public InterceptorAttachmentLevel AttachmentLevel { get; }
    public Character? AttachedCharacter { get; init; }
    public Site? AttachedSite { get; init; }
    public string? AttachedBlockId { get; init; }

    public HashSet<CommunicationType> SupportedTypes { get; } = new();
    public int Strength { get; set; } = 1;

    public Interceptor(string id, InterceptorAttachmentLevel attachmentLevel)
    {
        Id = id;
        AttachmentLevel = attachmentLevel;
    }

    public bool Supports(CommunicationType type) => SupportedTypes.Contains(type);
}

```

## `scripts/domain/communication/InterceptorAttachmentLevel.cs`

```csharp
namespace SurveillanceStategodot.scripts.domain.communication;

public enum InterceptorAttachmentLevel
{
    Character,
    Site,
    Block
}


```

## `scripts/domain/interrupt/CharacterInterrupt.cs`

```csharp
using SurveillanceStategodot.scripts.domain.assignment;

namespace SurveillanceStategodot.scripts.domain.interrupt;

/// <summary>
/// A runtime interrupt applied to a single character.
/// Created by any system that wants to override baseline schedule behavior
/// (e.g. RendezvousSystem, SurveillanceSystem).
/// </summary>
public sealed class CharacterInterrupt
{
    public string Id { get; }
    public InterruptType Type { get; }
    public InterruptPriority Priority { get; }
    public InterruptDisposition Disposition { get; }

    /// <summary>The character this interrupt targets.</summary>
    public Character Character { get; }

    /// <summary>
    /// The assignment that should be executed while this interrupt is active.
    /// Must be set before the interrupt is submitted via InterruptRequestedEvent.
    /// </summary>
    public Assignment ReplacementAssignment { get; }

    public bool IsActive { get; set; }

    public CharacterInterrupt(
        string id,
        InterruptType type,
        InterruptPriority priority,
        InterruptDisposition disposition,
        Character character,
        Assignment replacementAssignment)
    {
        Id = id;
        Type = type;
        Priority = priority;
        Disposition = disposition;
        Character = character;
        ReplacementAssignment = replacementAssignment;
    }
}


```

## `scripts/domain/interrupt/InterruptDisposition.cs`

```csharp
namespace SurveillanceStategodot.scripts.domain.interrupt;

public enum InterruptDisposition
{
    /// <summary>
    /// The current baseline assignment is saved and will be resumed when the interrupt clears.
    /// </summary>
    Suspend,

    /// <summary>
    /// The current baseline assignment is cancelled outright; schedule resumes from next entry.
    /// </summary>
    Replace
}


```

## `scripts/domain/interrupt/InterruptEvents.cs`

```csharp
using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.interrupt;

/// <summary>Published by any system that wants to apply an interrupt to a character.</summary>
public sealed record InterruptRequestedEvent(CharacterInterrupt Interrupt, double Time) : IDomainEvent;

/// <summary>Published after InterruptSystem has successfully applied the interrupt.</summary>
public sealed record InterruptAppliedEvent(CharacterInterrupt Interrupt, double Time) : IDomainEvent;

/// <summary>Published after an interrupt's assignment completes and the interrupt is cleared.</summary>
public sealed record InterruptClearedEvent(CharacterInterrupt Interrupt, double Time) : IDomainEvent;


```

## `scripts/domain/interrupt/InterruptPriority.cs`

```csharp
namespace SurveillanceStategodot.scripts.domain.interrupt;

public enum InterruptPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}


```

## `scripts/domain/interrupt/InterruptSystem.cs`

```csharp
using Godot;
using SurveillanceStategodot.scripts.domain.assignment;
using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.interrupt;

public sealed class InterruptSystem : ISimulationSystem
{
    private WorldState _world = null!;
    private SimulationEventBus _eventBus = null!;

    public void Initialize(WorldState world, SimulationEventBus eventBus)
    {
        _world = world;
        _eventBus = eventBus;

        _eventBus.Subscribe<InterruptRequestedEvent>(OnInterruptRequested);
        _eventBus.Subscribe<AssignmentCompletedEvent>(OnAssignmentCompleted);
    }

    public void Tick(double delta) { }

    private void OnInterruptRequested(InterruptRequestedEvent evt)
    {
        var interrupt = evt.Interrupt;
        var character = interrupt.Character;

        // Reject if an existing interrupt has equal or higher priority.
        if (character.ActiveInterrupt != null &&
            character.ActiveInterrupt.Priority >= interrupt.Priority)
        {
            GD.Print($"[InterruptSystem] Interrupt '{interrupt.Type}' rejected for '{character.Id}': " +
                     $"existing interrupt '{character.ActiveInterrupt.Type}' has equal or higher priority.");
            return;
        }

        // If there is an existing lower-priority interrupt active, clear it first.
        if (character.ActiveInterrupt != null)
        {
            ClearInterrupt(character.ActiveInterrupt, evt.Time, resumeBaseline: false);
        }

        // Determine what to do with the character's current active assignment.
        var currentAssignment = FindActiveAssignment(character);

        if (currentAssignment != null)
        {
            switch (interrupt.Disposition)
            {
                case InterruptDisposition.Suspend:
                    // Save baseline assignment so it can be resumed later.
                    character.SuspendedAssignment = currentAssignment;
                    CancelAssignment(currentAssignment, evt.Time);
                    break;

                case InterruptDisposition.Replace:
                    // Cancel without saving; schedule resumes from next entry when interrupt clears.
                    character.SuspendedAssignment = null;
                    CancelAssignment(currentAssignment, evt.Time);
                    break;
            }
        }

        // Apply the interrupt — state change before events.
        interrupt.IsActive = true;
        character.ActiveInterrupt = interrupt;

        // Mark the replacement assignment clearly.
        interrupt.ReplacementAssignment.Source = AssignmentSource.Interrupt;
        interrupt.ReplacementAssignment.InterruptId = interrupt.Id;

        _eventBus.Publish(new InterruptAppliedEvent(interrupt, evt.Time));
        _eventBus.Publish(new AssignmentCreatedEvent(interrupt.ReplacementAssignment, evt.Time));
    }

    private void OnAssignmentCompleted(AssignmentCompletedEvent evt)
    {
        var assignment = evt.Assignment;
        if (assignment.Source != AssignmentSource.Interrupt || assignment.InterruptId == null)
            return;

        var character = assignment.Character;
        if (character == null)
            return;

        var interrupt = character.ActiveInterrupt;
        if (interrupt == null || interrupt.Id != assignment.InterruptId)
            return;

        ClearInterrupt(interrupt, evt.Time, resumeBaseline: true);
    }

    private void ClearInterrupt(CharacterInterrupt interrupt, double time, bool resumeBaseline)
    {
        var character = interrupt.Character;

        // State change first.
        interrupt.IsActive = false;
        character.ActiveInterrupt = null;

        if (resumeBaseline)
        {
            var suspended = character.SuspendedAssignment;
            character.SuspendedAssignment = null;

            if (suspended != null)
            {
                // Re-issue the suspended baseline assignment from the start.
                suspended.Phase = AssignmentPhase.OutboundMovement;
                _eventBus.Publish(new AssignmentCreatedEvent(suspended, time));
            }
            // If no suspended assignment, character.ActiveInterrupt is now null and
            // ScheduleSystem.Tick will issue the next schedule entry on the next frame.
        }

        _eventBus.Publish(new InterruptClearedEvent(interrupt, time));
    }

    private void CancelAssignment(Assignment assignment, double time)
    {
        assignment.Phase = AssignmentPhase.Cancelled;
        _eventBus.Publish(new AssignmentCancelledEvent(assignment, time));
    }

    private Assignment FindActiveAssignment(Character character)
    {
        foreach (var assignment in _world.Assignments)
        {
            if (assignment.Character?.Id != character.Id)
                continue;
            if (assignment.Phase == AssignmentPhase.Completed || assignment.Phase == AssignmentPhase.Cancelled)
                continue;
            return assignment;
        }
        return null;
    }
}

```

## `scripts/domain/interrupt/InterruptType.cs`

```csharp
namespace SurveillanceStategodot.scripts.domain.interrupt;

/// <summary>
/// Describes the kind of reactive behavior that caused the interrupt.
/// Extend freely as new NPC reactions are added.
/// </summary>
public enum InterruptType
{
    // Surveillance / player-induced
    TailOpportunity,
    SuspiciousIntercept,
    TailAtRisk,
    ResistanceEncounter,
    ReactToSurveillance,

    // Coordination
    Rendezvous,

    // Operational security
    BugSweep,
    MoveStash,

    // Escape / evasion
    Flee,
    Relocate,

    // Lying low
    LayLow
}


```

## `scripts/domain/movement/Movement.cs`

```csharp
using System;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.navigation.query;

namespace SurveillanceStategodot.scripts.domain.movement;

/// <summary>
/// State container representing the intent to move from one place to another.
/// Does not own position data — authoritative position lives in Character.Position.
/// Advancement logic lives in MovementSystem, which supplies and receives position externally.
/// </summary>
public sealed class Movement
{
    public string Id { get; }
    public Character? Character { get; }
    public Site? Origin { get; }
    public Site? Destination { get; }
    public MovementMode Mode { get; private set; }

    /// <summary>For Pursuit mode: the character being followed.</summary>
    public Character? TargetCharacter { get; }

    /// <summary>
    /// True only for StaticPath movements when the end of the path is reached.
    /// Pursuit movements never self-arrive; AssignmentSystem cancels them externally.
    /// </summary>
    public bool HasArrived { get; private set; }

    public DispatchNavPath Path { get; private set; }

    /// <summary>
    /// Current segment index into Path.WorldPoints.
    /// Maintained by MovementSystem during advancement.
    /// </summary>
    public int SegmentIndex { get; set; }

    public event Action<Movement>? Arrived;

    // ── Static-path constructor ──────────────────────────────────────────────

    public Movement(
        string id,
        Character? character,
        Site? origin,
        Site? destination,
        DispatchNavPath path)
    {
        Id = id;
        Character = character;
        Origin = origin;
        Destination = destination;
        Mode = MovementMode.StaticPath;
        Path = path;
    }

    // ── Pursuit constructor ──────────────────────────────────────────────────

    public Movement(
        string id,
        Character? character,
        Site? origin,
        Character targetCharacter,
        DispatchNavPath initialPath)
    {
        Id = id;
        Character = character;
        Origin = origin;
        Destination = null;
        Mode = MovementMode.Pursuit;
        TargetCharacter = targetCharacter;
        Path = initialPath;
    }

    // ── Path replacement (used by MovementSystem for pursuit repathing) ──────

    public void ReplacePath(DispatchNavPath newPath)
    {
        Path = newPath;
        SegmentIndex = 0;
    }

    // ── Arrival ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Marks the movement as arrived and fires the Arrived event.
    /// Called by MovementSystem when the path end is reached, or externally via ForceArrive.
    /// </summary>
    public void MarkArrived()
    {
        if (HasArrived) return;
        HasArrived = true;
        Arrived?.Invoke(this);
    }

    /// <summary>
    /// Forces the movement to the arrived state.
    /// Used by AssignmentSystem to cancel pursuit once the target enters a site.
    /// </summary>
    public void ForceArrive() => MarkArrived();

    /// <summary>
    /// Converts a Pursuit movement into a StaticPath movement so it finishes
    /// traveling to its current path end and then self-arrives.
    /// Used when the target becomes stationary and the operator should close on
    /// the last-known position rather than repathing indefinitely.
    /// </summary>
    public void ConvertToStaticPath()
    {
        Mode = MovementMode.StaticPath;
    }
}

```

## `scripts/domain/movement/MovementEvents.cs`

```csharp
using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.movement;

public sealed record MovementStartedEvent(Movement Movement, double Time) : IDomainEvent;
public sealed record MovementArrivedEvent(Movement Movement, double Time) : IDomainEvent;
```

## `scripts/domain/movement/MovementMode.cs`

```csharp
namespace SurveillanceStategodot.scripts.domain.movement;

public enum MovementMode
{
    /// <summary>Normal movement along a pre-computed static path.</summary>
    StaticPath,

    /// <summary>
    /// Pursuit mode: the path is periodically recomputed toward a moving target.
    /// HasArrived is never set by the path end alone; AssignmentSystem decides when pursuit ends.
    /// </summary>
    Pursuit
}


```

## `scripts/domain/movement/MovementSystem.cs`

```csharp
using System.Collections.Generic;
using Godot;
using SurveillanceStategodot.scripts.domain.assignment;
using SurveillanceStategodot.scripts.domain.system;
using SurveillanceStategodot.scripts.navigation.authoring;
using SurveillanceStategodot.scripts.navigation.query;

namespace SurveillanceStategodot.scripts.domain.movement;

public sealed class MovementSystem : ISimulationSystem
{
    private readonly DispatchNav _dispatchNav;

    private WorldState _world = null!;
    private SimulationEventBus _eventBus = null!;

    private readonly List<Movement> _activeMovements = new();

    public const float DefaultSpeed = 3f;

    private enum PursuitZone { Far, Matched, Close }
    private readonly Dictionary<string, PursuitZone> _pursuitZone = new();

    public MovementSystem(DispatchNav dispatchNav)
    {
        _dispatchNav = dispatchNav;
    }

    public void Initialize(WorldState world, SimulationEventBus eventBus)
    {
        _world = world;
        _eventBus = eventBus;

        _eventBus.Subscribe<MovementStartedEvent>(OnMovementStarted);
        _eventBus.Subscribe<AssignmentCancelledEvent>(OnAssignmentCancelled);
    }

    public void Tick(double delta)
    {
        for (int i = _activeMovements.Count - 1; i >= 0; i--)
        {
            var movement = _activeMovements[i];

            // Pursuit: update path endpoint to target's current position every frame.
            if (movement.Mode == MovementMode.Pursuit)
            {
                UpdatePursuitTarget(movement);
            }

            var speed = ResolveSpeed(movement);
            AdvanceMovement(movement, speed * (float)delta);

            if (!movement.HasArrived)
                continue;

            _activeMovements.RemoveAt(i);
            _pursuitZone.Remove(movement.Id);

            if (movement.Character != null)
            {
                if (movement.Character.CurrentMovement == movement)
                    movement.Character.CurrentMovement = null;
                movement.Character.CurrentSite = movement.Destination;

                if (movement.Destination != null)
                {
                    movement.Destination.AddOccupant(movement.Character);

                    // Update authoritative position to the site entry point.
                    movement.Character.Position.Set(movement.Destination.EntryPosition);

                    var prevOnArrival = movement.Character.LocationType;
                    movement.Character.LocationType = CharacterLocationType.Site;
                    _eventBus.Publish(new CharacterLocationChangedEvent(
                        movement.Character,
                        prevOnArrival,
                        CharacterLocationType.Site,
                        _world.Time));
                }
            }

            _eventBus.Publish(new MovementArrivedEvent(movement, _world.Time));
        }
    }

    /// <summary>
    /// Returns the effective travel speed for this tick.
    ///
    /// During pursuit, three zones relative to the operator's vision range apply:
    ///   dist > 0.55 vr  →  full own speed (closing in)
    ///   dist 0.45–0.55  →  match target speed (shadowing)
    ///   dist &lt; 0.45 vr  →  half target speed (backing off without changing path)
    ///
    /// Hysteresis: each zone is entered at its threshold but only exited when
    /// the distance reaches the midpoint (0.5 vr), preventing flickering.
    /// </summary>
    private float ResolveSpeed(Movement movement)
    {
        var character = movement.Character;
        var ownSpeed = character?.MovementSpeed ?? DefaultSpeed;

        if (movement.Mode != MovementMode.Pursuit ||
            character == null ||
            movement.TargetCharacter == null)
            return ownSpeed;

        var visionRange = character.VisionRange;
        var dist = character.Position.WorldPosition
            .DistanceTo(movement.TargetCharacter.Position.WorldPosition);
        var targetSpeed = movement.TargetCharacter.MovementSpeed;

        // Determine current zone with hysteresis exit at 0.5 vr.
        var current = _pursuitZone.GetValueOrDefault(movement.Id, PursuitZone.Far);
        PursuitZone next;

        switch (current)
        {
            case PursuitZone.Far:
                next = dist <= visionRange * 0.55f ? PursuitZone.Matched : PursuitZone.Far;
                break;
            case PursuitZone.Matched:
                if (dist < visionRange * 0.45f)      next = PursuitZone.Close;
                else if (dist > visionRange * 0.55f) next = PursuitZone.Far;
                else                                  next = PursuitZone.Matched;
                break;
            case PursuitZone.Close:
                next = dist >= visionRange * 0.45f ? PursuitZone.Matched : PursuitZone.Close;
                break;
            default:
                next = PursuitZone.Far;
                break;
        }

        _pursuitZone[movement.Id] = next;

        return next switch
        {
            PursuitZone.Far     => ownSpeed,
            PursuitZone.Matched => Mathf.Min(ownSpeed, targetSpeed),
            PursuitZone.Close   => targetSpeed * 0.5f,
            _                   => ownSpeed
        };
    }

    /// <summary>
    /// Advances a movement by reading the character's authoritative position,
    /// stepping along the path, and writing the result back to Character.Position.
    /// SegmentIndex on the movement is kept up to date here.
    /// </summary>
    private static void AdvanceMovement(Movement movement, float travelDistance)
    {
        if (movement.HasArrived || !movement.Path.IsValid || movement.Path.WorldPoints.Count < 2)
            return;

        var character = movement.Character;
        var currentPos = character?.Position.WorldPosition ?? movement.Path.StartPosition;

        var result = movement.Path.Advance(movement.SegmentIndex, currentPos, travelDistance);

        movement.SegmentIndex = result.SegmentIndex;

        if (character != null && currentPos != result.Position)
        {
            character.Position.Update(result.Position, result.Direction);
        }

        // Only StaticPath movements self-arrive.
        if (movement.Mode == MovementMode.StaticPath && result.ReachedDestination)
        {
            movement.MarkArrived();
        }
    }

    /// <summary>
    /// Updates the pursuit path endpoint to the target's current position every frame.
    /// Always does a full repath so the path endpoint exactly tracks the target —
    /// this avoids the "stops short" issue caused by edge-slide heuristics.
    /// </summary>
    private void UpdatePursuitTarget(Movement movement)
    {
        var target = movement.TargetCharacter;
        if (target == null)
            return;

        var targetPos = target.CurrentSite != null
            ? target.CurrentSite.EntryPosition
            : target.Position.WorldPosition;

        var newPath = DispatchNavPathfinder.FindPath(
            _dispatchNav.Graph,
            movement.Character?.Position.WorldPosition ?? movement.Path.StartPosition,
            targetPos);

        if (newPath.IsValid)
            movement.ReplacePath(newPath);
        else
            GD.PushWarning($"[MovementSystem] Pursuit repath failed for movement {movement.Id}.");
    }

    private void OnMovementStarted(MovementStartedEvent evt)
    {
        if (!_activeMovements.Contains(evt.Movement))
        {
            _activeMovements.Add(evt.Movement);
        }

        if (evt.Movement.Character != null)
        {
            var character = evt.Movement.Character;
            var previousSite = character.CurrentSite;

            character.CurrentMovement = evt.Movement;

            // Seed the authoritative position from the path's start position.
            character.Position.Set(evt.Movement.Path.StartPosition);

            if (previousSite != null)
            {
                previousSite.RemoveOccupant(character);
            }

            character.CurrentSite = null;

            var prevOnStart = character.LocationType;
            character.LocationType = CharacterLocationType.NavGraph;
            _eventBus.Publish(new CharacterLocationChangedEvent(
                character,
                prevOnStart,
                CharacterLocationType.NavGraph,
                _world.Time));
        }
    }

    private void OnAssignmentCancelled(AssignmentCancelledEvent evt)
    {
        var movement = evt.Assignment.CurrentMovement;
        if (movement == null)
            return;

        _activeMovements.Remove(movement);
        _pursuitZone.Remove(movement.Id);

        if (movement.Character != null)
        {
            movement.Character.CurrentMovement = null;
        }
    }
}

```

## `scripts/domain/observation/AggregatedObservationLogEntry.cs`

```csharp
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.domain.observation;

public sealed class AggregatedObservationLogEntry
{
    public ObservationLogKey Key { get; }
    public ObservationType ObservationType => Key.ObservationType;
    public string SiteLabel { get; }
    public string CharacterLabel { get; }
    public string OperationLabel { get; }

    /// <summary>
    /// The highest (worst) compliance level observed across all occurrences.
    /// NonCompliant > Suspicious > Compliant.
    /// </summary>
    public ComplianceType ComplianceType { get; private set; }

    public int Count { get; private set; }
    public double FirstSeenTime { get; private set; }
    public double LastSeenTime { get; private set; }

    public AggregatedObservationLogEntry(
        ObservationLogKey key,
        string siteLabel,
        string characterLabel,
        string operationLabel,
        double firstSeenTime,
        ComplianceType complianceType = ComplianceType.Compliant)
    {
        Key = key;
        SiteLabel = siteLabel;
        CharacterLabel = characterLabel;
        OperationLabel = operationLabel;
        Count = 1;
        FirstSeenTime = firstSeenTime;
        LastSeenTime = firstSeenTime;
        ComplianceType = complianceType;
    }

    public void AddOccurrence(double time, ComplianceType complianceType)
    {
        Count++;
        if (time < FirstSeenTime) FirstSeenTime = time;
        if (time > LastSeenTime)  LastSeenTime  = time;
        // Escalate compliance level — never downgrade.
        if (complianceType > ComplianceType) ComplianceType = complianceType;
    }
}
```

## `scripts/domain/observation/Observation.cs`

```csharp
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.domain.observation;

public sealed class Observation
{
    public string Id { get; }
    public string? SiteId { get; }
    public string? CharacterId { get; }
    public string? OperationId { get; }
    public double Time { get; }
    public ObservationType ObservationType { get; }
    public ComplianceType ComplianceType { get; }

    // Optional label snapshots for UI convenience.
    public string? SiteLabelSnapshot { get; }
    public string? CharacterLabelSnapshot { get; }
    public string? OperationLabelSnapshot { get; }

    public Observation(
        string id,
        string? siteId,
        string? characterId,
        string? operationId,
        double time,
        ObservationType observationType,
        ComplianceType complianceType = ComplianceType.Compliant,
        string? siteLabelSnapshot = null,
        string? characterLabelSnapshot = null,
        string? operationLabelSnapshot = null)
    {
        Id = id;
        SiteId = siteId;
        CharacterId = characterId;
        OperationId = operationId;
        Time = time;
        ObservationType = observationType;
        ComplianceType = complianceType;
        SiteLabelSnapshot = siteLabelSnapshot;
        CharacterLabelSnapshot = characterLabelSnapshot;
        OperationLabelSnapshot = operationLabelSnapshot;
    }
}
```

## `scripts/domain/observation/ObservationEvents.cs`

```csharp
using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.observation;

public sealed record ObservationCreatedEvent(Observation Observation, double Time) : IDomainEvent;
```

## `scripts/domain/observation/ObservationLogKey.cs`

```csharp
namespace SurveillanceStategodot.scripts.domain.observation;

public readonly record struct ObservationLogKey(
    string? SiteId,
    string? CharacterId,
    ObservationType ObservationType
);

```

## `scripts/domain/observation/ObservationLogSystem.cs`

```csharp
using System;
using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.observation;

public sealed class ObservationLogSystem : ISimulationSystem
{
    private readonly Dictionary<ObservationLogKey, AggregatedObservationLogEntry> _entries = new();

    // Observations collected this tick, deduplicated before being committed to the log.
    private readonly List<Observation> _pending = new();

    private SimulationEventBus _eventBus = null!;

    public IReadOnlyDictionary<ObservationLogKey, AggregatedObservationLogEntry> Entries => _entries;

    public event Action<AggregatedObservationLogEntry>? EntryAdded;
    public event Action<AggregatedObservationLogEntry>? EntryUpdated;

    public void Initialize(WorldState world, SimulationEventBus eventBus)
    {
        _eventBus = eventBus;
        _eventBus.Subscribe<ObservationCreatedEvent>(OnObservationCreated);
    }

    public void Tick(double delta)
    {
        if (_pending.Count == 0)
            return;

        // Deduplicate pending observations by log key, escalating compliance.
        var deduped = new Dictionary<ObservationLogKey, Observation>();
        foreach (var obs in _pending)
        {
            var key = MakeKey(obs);
            if (deduped.TryGetValue(key, out var existing))
            {
                // Keep the observation with the worst compliance level.
                if (obs.ComplianceType > existing.ComplianceType)
                    deduped[key] = obs;
            }
            else
            {
                deduped[key] = obs;
            }
        }

        _pending.Clear();

        // Commit deduplicated observations to the log.
        foreach (var (key, obs) in deduped)
        {
            if (_entries.TryGetValue(key, out var existing))
            {
                existing.AddOccurrence(obs.Time, obs.ComplianceType);
                EntryUpdated?.Invoke(existing);
            }
            else
            {
                var entry = new AggregatedObservationLogEntry(
                    key,
                    siteLabel: obs.SiteLabelSnapshot ?? "Unknown Site",
                    characterLabel: obs.CharacterLabelSnapshot ?? "Unknown Character",
                    operationLabel: obs.OperationLabelSnapshot ?? "Unknown Operation",
                    firstSeenTime: obs.Time,
                    complianceType: obs.ComplianceType);

                _entries.Add(key, entry);
                EntryAdded?.Invoke(entry);
            }
        }
    }

    private void OnObservationCreated(ObservationCreatedEvent evt)
    {
        _pending.Add(evt.Observation);
    }

    private static ObservationLogKey MakeKey(Observation obs) =>
        new(
            obs.SiteId,
            obs.CharacterId,
            obs.ObservationType);
}
```

## `scripts/domain/observation/ObservationType.cs`

```csharp
namespace SurveillanceStategodot.scripts.domain.observation;

public enum ObservationType
{
    SpottedMoving,
    /// <summary>Character observed inside a site by a stakeout or watch source.</summary>
    SpottedAtSite
}


```

## `scripts/domain/operation/ComplianceType.cs`

```csharp
namespace SurveillanceStategodot.scripts.domain.operation;

public enum ComplianceType
{
    Compliant,
    Suspicious,
    NonCompliant
}
```

## `scripts/domain/operation/Operation.cs`

```csharp
using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain.movement;

namespace SurveillanceStategodot.scripts.domain.operation;

public sealed class Operation
{
    public string Id { get; }
    public string Label { get; }
    public OperationState State { get; set; } = OperationState.Planned;

    public ComplianceType ComplianceType { get; }

    public List<Character> Participants { get; } = new();
    public Site? SiteContext { get; set; }
    public Movement? MovementContext { get; set; }

    public double StartTime { get; private set; }
    public double Duration { get; }
    public OperationVisionType VisionType { get; }
    public HashSet<OperationObservationTag> ObservationTags { get; } = new();

    public double EndTime => StartTime + Duration;

    public Operation(string id, string label, double duration, OperationVisionType visionType = OperationVisionType.None, ComplianceType complianceType = ComplianceType.Compliant)
    {
        Id = id;
        Label = label;
        Duration = duration;
        VisionType = visionType;
        ComplianceType = complianceType;
    }
    
    public void Start(double worldTime)
    {
        State = OperationState.Active;
        StartTime = worldTime;
    }

    public bool IsComplete(double worldTime) => worldTime >= EndTime;
}
```

## `scripts/domain/operation/OperationEvents.cs`

```csharp
using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.operation;

public sealed record OperationStartedEvent(Operation Operation, double Time) : IDomainEvent;
public sealed record OperationCompletedEvent(Operation Operation, double Time) : IDomainEvent;
```

## `scripts/domain/operation/OperationObservationTag.cs`

```csharp
namespace SurveillanceStategodot.scripts.domain.operation;

public enum OperationObservationTag
{
    Sleeping,
    Working,
    Meeting,
    SuspiciousActivity,
    PhoneConversation,
    WeaponsTransfer,
    PlanningAttack
}


```

## `scripts/domain/operation/OperationState.cs`

```csharp
namespace SurveillanceStategodot.scripts.domain.operation;

public enum OperationState
{
    Planned,
    Active,
    Completed,
    Cancelled
}


```

## `scripts/domain/operation/OperationSystem.cs`

```csharp
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
            operation.SiteContext?.RemoveActiveOperation(operation);

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
```

## `scripts/domain/operation/OperationVisionType.cs`

```csharp
namespace SurveillanceStategodot.scripts.domain.operation;

public enum OperationVisionType
{
    None,
    Stakeout
}


```

## `scripts/domain/operation/Site.cs`

```csharp
using System;
using System.Collections.Generic;
using Godot;
using SurveillanceStategodot.scripts.domain.assignment;
using SurveillanceStategodot.scripts.domain.communication;
using SurveillanceStategodot.scripts.navigation.query;

namespace SurveillanceStategodot.scripts.domain.operation;

public sealed class Site
{
    private readonly List<Character> _occupants = new();
    private readonly List<SiteAsset> _assets = new();
    private readonly List<Operation> _activeOperations = new();
    private readonly List<Interceptor> _interceptors = new();

    public string Id { get; }
    public string Label { get; }
    public string BuildingId { get; }
    public SiteVisibility Visibility { get; set; } = SiteVisibility.Hidden;
    public string? BlockId { get; set; }
    public Vector3 GlobalPosition { get; set; }

    /// <summary>
    /// The closest point on the nav-graph to this site's world position.
    /// Precomputed once during bootstrapping.
    /// Used for pathfinding start/end anchors and as the world position for fixed VisionSources.
    /// Null until the nav graph has been stamped (see ScenarioBootstrapper).
    /// </summary>
    public DispatchNavEdgeAnchor? NavAnchor { get; set; }

    /// <summary>
    /// The world position agents navigate toward when heading to this site.
    /// Falls back to GlobalPosition when NavAnchor has not been stamped yet.
    /// </summary>
    public Vector3 EntryPosition => NavAnchor.HasValue ? NavAnchor.Value.Position : GlobalPosition;

    public Option[] AvailableOptions { get; set; } = [];

    public IReadOnlyList<Character> Occupants => _occupants;
    public IReadOnlyList<SiteAsset> Assets => _assets;
    public IReadOnlyList<Operation> ActiveOperations => _activeOperations;
    public IReadOnlyList<Interceptor> Interceptors => _interceptors;

    public event Action<Site, Operation>? ActiveOperationAdded;
    public event Action<Site, Operation>? ActiveOperationRemoved;
    
    public event Action<Site, Character>? OccupantAdded;
    public event Action<Site, Character>? OccupantRemoved;

    public Site(
        string id,
        string label,
        string buildingId,
        Vector3 globalPosition,
        Option[]? availableOptions = null)
    {
        Id = id;
        Label = label;
        BuildingId = buildingId;
        GlobalPosition = globalPosition;
        AvailableOptions = availableOptions ?? [];
    }

    public bool AddActiveOperation(Operation operation)
    {
        if (_activeOperations.Contains(operation))
            return false;

        _activeOperations.Add(operation);
        ActiveOperationAdded?.Invoke(this, operation);
        return true;
    }

    public bool RemoveActiveOperation(Operation operation)
    {
        if (!_activeOperations.Remove(operation))
            return false;

        ActiveOperationRemoved?.Invoke(this, operation);
        return true;
    }

    public bool AddOccupant(Character character)
    {
        if (_occupants.Contains(character))
            return false;

        _occupants.Add(character);
        OccupantAdded?.Invoke(this, character);
        return true;
    }

    public bool RemoveOccupant(Character character)
    {
        if(!_occupants.Remove(character))
            return false;
        
        OccupantRemoved?.Invoke(this, character);
        return true;
    }

    public bool AddAsset(SiteAsset asset)
    {
        if (_assets.Contains(asset))
            return false;

        _assets.Add(asset);
        return true;
    }

    public bool AddInterceptor(Interceptor interceptor)
    {
        if (_interceptors.Contains(interceptor))
            return false;

        _interceptors.Add(interceptor);
        return true;
    }
}
```

## `scripts/domain/plot/Plot.cs`

```csharp
using System.Collections.Generic;

namespace SurveillanceStategodot.scripts.domain.plot;

public class Plot
{
    public bool Initialized = false;
    
    public string Id { get; }
    public string Label { get; }

    public List<Character> Characters { get; } = new();

    public Plot(string id, string label)
    {
        Id = id;
        Label = label;
    }
}
```

## `scripts/domain/plot/PlotSystem.cs`

```csharp
using System.Collections.Generic;
using SurveillanceStategodot.scripts.authoring;
using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.plot;

public sealed class PlotSystem : ISimulationSystem
{
    private readonly IReadOnlyList<PlotResource> _plotDefinitions;

    private WorldState _world = null!;
    private bool _initialized;

    public void Initialize(WorldState world, SimulationEventBus eventBus)
    {
        _world = world;
    }

    public void Tick(double delta)
    {
        foreach (var plot in _world.Plots)
        {
            if (!plot.Initialized)
            {
                InitializePlot(plot);
            }
        }
    }
    
    private void InitializePlot(Plot plot)
    {
        foreach (var character in plot.Characters)
        {
            var firstScheduleEntry = character.Schedule?.Entries[0];
            if (firstScheduleEntry != null)
            {
                var initialSite = _world.GetSite(firstScheduleEntry.SiteId);
                character.CurrentSite = initialSite;
                character.LocationType = CharacterLocationType.Site;
                initialSite.AddOccupant(character);
            }
        }

        plot.Initialized = true;
    }
}
```

## `scripts/domain/schedule/Schedule.cs`

```csharp
using System.Collections.Generic;

namespace SurveillanceStategodot.scripts.domain.schedule;

/// <summary>
/// Baseline looping routine for an NPC character.
/// ScheduleSystem reads this to generate assignments when the character is idle.
/// </summary>
public sealed class Schedule
{
    public IReadOnlyList<ScheduleEntry> Entries { get; }

    private int _currentIndex;

    public bool HasEntries => Entries.Count > 0;

    public Schedule(IReadOnlyList<ScheduleEntry> entries)
    {
        Entries = entries;
        _currentIndex = 0;
    }

    /// <summary>
    /// Returns the current entry and advances the index, looping back to 0.
    /// </summary>
    public ScheduleEntry Advance()
    {
        var entry = Entries[_currentIndex];
        _currentIndex = (_currentIndex + 1) % Entries.Count;
        return entry;
    }

    /// <summary>Current entry without advancing the index.</summary>
    public ScheduleEntry Current => Entries[_currentIndex];
}


```

## `scripts/domain/schedule/ScheduleEntry.cs`

```csharp
using System;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.domain.schedule;

/// <summary>
/// A single step in an NPC's baseline routine.
/// SiteId is resolved to a runtime Site via WorldState at execution time.
/// </summary>
public sealed class ScheduleEntry
{
    /// <summary>Runtime site ID to look up in WorldState.</summary>
    public string SiteId { get; }

    /// <summary>Human-readable label for the operation performed at the site.</summary>
    public string OperationLabel { get; }

    /// <summary>How long the NPC dwells / operates at the site (world-time seconds).</summary>
    public double Duration { get; }
    
    public ComplianceType ComplianceType { get; }

    public ScheduleEntry(string siteId, string operationLabel, double duration, ComplianceType complianceType)
    {
        SiteId = siteId;
        OperationLabel = operationLabel;
        Duration = duration;
        ComplianceType = complianceType;
    }
    
    public Operation ToOperation(Site site, Movement movement)
    {
        return new Operation(
            id: Guid.NewGuid().ToString(),
            label: OperationLabel,
            duration: Duration,
            visionType: OperationVisionType.None,
            complianceType: ComplianceType)
        {
            SiteContext = site,
            MovementContext = movement
        };
    }
}


```

## `scripts/domain/schedule/ScheduleSystem.cs`

```csharp
using System;
using Godot;
using SurveillanceStategodot.scripts.domain.assignment;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.domain.system;
using SurveillanceStategodot.scripts.navigation.authoring;
using SurveillanceStategodot.scripts.navigation.query;

namespace SurveillanceStategodot.scripts.domain.schedule;

public sealed class ScheduleSystem : ISimulationSystem
{
    private readonly DispatchNav _dispatchNav;

    private WorldState _world = null!;
    private SimulationEventBus _eventBus = null!;

    public ScheduleSystem(DispatchNav dispatchNav)
    {
        _dispatchNav = dispatchNav;
    }

    public void Initialize(WorldState world, SimulationEventBus eventBus)
    {
        _world = world;
        _eventBus = eventBus;
    }

    public void Tick(double delta)
    {
        foreach (var character in _world.Characters)
        {
            if (character.Schedule == null || !character.Schedule.HasEntries)
                continue;

            // Skip characters under an active interrupt — InterruptSystem owns their assignment slot.
            if (character.ActiveInterrupt != null)
                continue;

            if (_world.HasActiveAssignmentForCharacter(character.Id))
                continue;

            IssueNextScheduleAssignment(character);
        }
    }

    private void IssueNextScheduleAssignment(Character character)
    {
        var schedule = character.Schedule!;
        var entry = schedule.Advance();

        if (!_world.TryGetSite(entry.SiteId, out var site))
        {
            GD.PushWarning($"[ScheduleSystem] Site '{entry.SiteId}' not found for character '{character.Id}'. Skipping entry.");
            return;
        }

        Vector3 startPosition;
        if (character.CurrentSite != null)
        {
            startPosition = character.CurrentSite.EntryPosition;
        }
        else if (DispatchNavSpawnQueries.TryGetSpawnPoint(_dispatchNav.Graph, site.EntryPosition, out var spawn))
        {
            startPosition = spawn.Position;
        }
        else
        {
            startPosition = site.EntryPosition;
        }

        var path = site.NavAnchor.HasValue
            ? DispatchNavPathfinder.FindPath(_dispatchNav.Graph, startPosition, site.NavAnchor.Value)
            : DispatchNavPathfinder.FindPath(_dispatchNav.Graph, startPosition, site.GlobalPosition);

        if (!path.IsValid)
        {
            GD.PushWarning($"[ScheduleSystem] No valid path to site '{entry.SiteId}' for character '{character.Id}'. Skipping entry.");
            return;
        }

        var movement = new Movement(
            id: Guid.NewGuid().ToString(),
            character: character,
            origin: character.CurrentSite,
            destination: site,
            path: path);

        var operation = entry.ToOperation(site, movement);

        var assignment = new Assignment(
            id: Guid.NewGuid().ToString(),
            character: character,
            operation: operation,
            currentMovement: movement)
        {
            CompletionBehavior = AssignmentCompletionBehavior.AwaitSchedule,
            Source = AssignmentSource.Schedule
        };

        _eventBus.Publish(new AssignmentCreatedEvent(assignment, _world.Time));
    }
}

```

## `scripts/domain/system/IDomainEvent.cs`

```csharp
namespace SurveillanceStategodot.scripts.domain.system;

public interface IDomainEvent
{
    double Time { get; }
}
```

## `scripts/domain/system/ISimulationSystem.cs`

```csharp
namespace SurveillanceStategodot.scripts.domain.system;

public interface ISimulationSystem
{
    void Initialize(WorldState world, SimulationEventBus eventBus);
    void Tick(double delta);
}
```

## `scripts/domain/system/SimulationEventBus.cs`

```csharp
using System;
using System.Collections.Generic;

namespace SurveillanceStategodot.scripts.domain.system;

public sealed class SimulationEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();

    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IDomainEvent
    {
        var type = typeof(TEvent);
        if (!_handlers.TryGetValue(type, out var handlers))
        {
            handlers = new List<Delegate>();
            _handlers[type] = handlers;
        }

        handlers.Add(handler);
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IDomainEvent
    {
        var type = typeof(TEvent);
        if (_handlers.TryGetValue(type, out var handlers))
        {
            handlers.Remove(handler);
            if (handlers.Count == 0)
            {
                _handlers.Remove(type);
            }
        }
    }

    public void Publish<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        var type = typeof(TEvent);
        if (!_handlers.TryGetValue(type, out var handlers))
            return;

        // Copy to avoid modification-during-iteration issues.
        var snapshot = handlers.ToArray();
        foreach (var handler in snapshot)
        {
            ((Action<TEvent>)handler)(domainEvent);
        }
    }
}
```

## `scripts/domain/system/WorldState.cs`

```csharp
using System;
using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain.assignment;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.domain.plot;
using SurveillanceStategodot.scripts.domain.vision;

namespace SurveillanceStategodot.scripts.domain.system;

public sealed class WorldState
{
    public double Time { get; private set; }

    public event Action<List<Character>> OnOperatorsChanged;

    public List<Site> Sites { get; } = new();
    public List<Character> Characters { get; } = new();
    public List<Character> Operators { get; } = new();
    public List<Assignment> Assignments { get; } = new();
    public List<Plot> Plots { get; } = new();

    // Vision sources are authoritative runtime state owned by WorldState.
    private readonly Dictionary<string, VisionSource> _visionSourcesById = new();
    public IReadOnlyDictionary<string, VisionSource> VisionSources => _visionSourcesById;

    public event Action<VisionSource>? VisionSourceAdded;
    public event Action<VisionSource>? VisionSourceRemoved;

    private readonly Dictionary<string, Site> _sitesById = new();
    private readonly Dictionary<string, Character> _charactersById = new();
    public IReadOnlyDictionary<string, Site> SitesById => _sitesById;

    private readonly Dictionary<string, Assignment> _assignmentsById = new();
    // NOTE: operation id index is kept updated via UpdateOperationIndex / RemoveOperationIndex.
    private readonly Dictionary<string, Assignment> _assignmentsByOperationId = new();
    // Index: target character id -> tail assignment (one tail per character at a time).
    private readonly Dictionary<string, Assignment> _assignmentsByTargetCharacterId = new();

    public void AdvanceTime(double delta)
    {
        Time += delta;
    }

    public void RegisterSite(Site site)
    {
        if (_sitesById.ContainsKey(site.Id))
            return;

        _sitesById.Add(site.Id, site);
        Sites.Add(site);
    }

    public Site GetSite(string id)
    {
        return _sitesById[id];
    }

    public Character GetCharacter(string id)
    {
        return _charactersById[id];
    }

    public bool TryGetSite(string id, out Site? site)
    {
        return _sitesById.TryGetValue(id, out site);
    }

    public void RegisterCharacter(Character character)
    {
        if (_charactersById.ContainsKey(character.Id))
            return;

        _charactersById.Add(character.Id, character);
        Characters.Add(character);
    }

    public void RegisterOperator(Character character)
    {
        character.IsOperator = true;

        if (_charactersById.ContainsKey(character.Id))
            return;

        _charactersById.Add(character.Id, character);
        Characters.Add(character);
        Operators.Add(character);
        
        OnOperatorsChanged?.Invoke(Operators);
    }

    public void RegisterAssignment(Assignment assignment)
    {
        if (!Assignments.Contains(assignment))
        {
            Assignments.Add(assignment);
        }

        _assignmentsById[assignment.Id] = assignment;

        if (assignment.CurrentOperation != null)
            _assignmentsByOperationId[assignment.CurrentOperation.Id] = assignment;

        if (assignment.TargetCharacter != null)
            _assignmentsByTargetCharacterId[assignment.TargetCharacter.Id] = assignment;
    }

    /// <summary>
    /// Call whenever assignment.CurrentOperation changes so the lookup stays consistent.
    /// </summary>
    public void UpdateAssignmentOperationIndex(Assignment assignment, string? oldOperationId)
    {
        if (oldOperationId != null)
            _assignmentsByOperationId.Remove(oldOperationId);

        if (assignment.CurrentOperation != null)
            _assignmentsByOperationId[assignment.CurrentOperation.Id] = assignment;
    }

    public void RegisterPlot(Plot plot)
    {
        if (!Plots.Contains(plot))
        {
            Plots.Add(plot);
        }

        foreach (var character in plot.Characters)
        {
            RegisterCharacter(character);
        }
    }

    public bool TryGetAssignmentByOperationId(string operationId, out Assignment assignment)
    {
        return _assignmentsByOperationId.TryGetValue(operationId, out assignment!);
    }

    public bool TryGetAssignmentByMovementId(string movementId, out Assignment? assignment)
    {
        foreach (var candidate in Assignments)
        {
            if (candidate.CurrentMovement?.Id == movementId)
            {
                assignment = candidate;
                return true;
            }
        }

        assignment = null;
        return false;
    }

    /// <summary>
    /// Returns the active TailCharacter assignment targeting the given character, if any.
    /// </summary>
    public bool TryGetTailAssignmentForTarget(string targetCharacterId, out Assignment? assignment)
    {
        if (_assignmentsByTargetCharacterId.TryGetValue(targetCharacterId, out var found) &&
            found.Phase is not (AssignmentPhase.Completed or AssignmentPhase.Cancelled or AssignmentPhase.Failed))
        {
            assignment = found;
            return true;
        }

        assignment = null;
        return false;
    }

    /// <summary>
    /// Returns true when a character has an assignment that is not yet completed or cancelled.
    /// </summary>
    public bool HasActiveAssignmentForCharacter(string characterId)
    {
        return GetActiveAssignmentForCharacter(characterId) != null;
    }

    /// <summary>
    /// Returns the first non-completed, non-cancelled assignment for the given character, or null.
    /// </summary>
    public Assignment? GetActiveAssignmentForCharacter(string characterId)
    {
        foreach (var assignment in Assignments)
        {
            if (assignment.Character?.Id != characterId)
                continue;

            if (assignment.Phase is AssignmentPhase.Completed or AssignmentPhase.Cancelled or AssignmentPhase.Failed)
                continue;

            return assignment;
        }

        return null;
    }

    public void RegisterVisionSource(VisionSource source)
    {
        _visionSourcesById[source.Id] = source;
        VisionSourceAdded?.Invoke(source);
    }

    public void RemoveVisionSource(string id)
    {
        if (_visionSourcesById.TryGetValue(id, out var source))
        {
            _visionSourcesById.Remove(id);
            source.Deactivate();
            VisionSourceRemoved?.Invoke(source);
        }
    }

    public bool TryGetVisionSource(string id, out VisionSource? source)
    {
        return _visionSourcesById.TryGetValue(id, out source);
    }
}


```

## `scripts/domain/vision/VisionSource.cs`

```csharp
using System;
using Godot;
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.domain.vision;

public sealed class VisionSource
{
    public string Id { get; }
    public Character? Owner { get; }
    public VisionSourceType Type { get; }
    public float Range { get; private set; }
    public Vector3 WorldPosition { get; private set; }
    public Site? SiteContext { get; private set; }
    public bool IsMapVisible { get; private set; }
    public bool IsActive { get; private set; } = true;

    public event Action<VisionSource>? Changed;
    public event Action<VisionSource>? Deactivated;

    public VisionSource(string id, Character? owner, VisionSourceType type, float range, bool isMapVisible = false)
    {
        Id = id;
        Owner = owner;
        Type = type;
        Range = range;
        IsMapVisible = isMapVisible;
    }

    public void SetWorldPosition(Vector3 position)
    {
        WorldPosition = position;
        Changed?.Invoke(this);
    }

    public void SetRange(float range)
    {
        Range = range;
        Changed?.Invoke(this);
    }

    public void SetSiteContext(Site? site)
    {
        SiteContext = site;
        Changed?.Invoke(this);
    }

    public void SetMapVisible(bool visible)
    {
        IsMapVisible = visible;
        Changed?.Invoke(this);
    }

    public void Deactivate()
    {
        IsActive = false;
        Deactivated?.Invoke(this);
    }
}
```

## `scripts/domain/vision/VisionSourceType.cs`

```csharp
namespace SurveillanceStategodot.scripts.domain.vision;

public enum VisionSourceType
{
    /// <summary>
    /// Unified operator map-level vision source. Active while the operator is on
    /// the nav graph (CurrentSite == null). Can see moving NPCs, site occupants,
    /// and operations within range. Removed when the operator enters a site;
    /// recreated when they exit.
    /// </summary>
    OperatorPresence
}
```

## `scripts/domain/vision/VisionSourceTypeExt.cs`

```csharp
namespace SurveillanceStategodot.scripts.domain.vision;

public static class VisionSourceTypeExt
{
    /// <summary>
    /// Whether this source can observe active operations at sites within its range.
    /// Map-level: can detect that something is happening and whether it looks suspicious,
    /// but cannot determine the specific operation type.
    /// </summary>
    public static bool CanSeeOperations(this VisionSourceType type) => type switch
    {
        VisionSourceType.OperatorPresence => true,
        _ => false
    };

    /// <summary>
    /// Whether this source can observe which characters are occupying a site within its range.
    /// </summary>
    public static bool CanSeeOccupants(this VisionSourceType type) => type switch
    {
        VisionSourceType.OperatorPresence => true,
        _ => false
    };

    /// <summary>
    /// Whether this source can determine that a specific operation is non-compliant.
    /// Map-level sources can only mark activities as Suspicious; a close-inspection
    /// source would be needed to confirm NonCompliant.
    /// </summary>
    public static bool CanDetectNonCompliance(this VisionSourceType type) => type switch
    {
        VisionSourceType.OperatorPresence => false,
        _ => false
    };
}

```

## `scripts/domain/vision/VisionSystem.cs`

```csharp
using System;
using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.observation;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.vision;

public sealed class VisionSystem : ISimulationSystem
{
    private WorldState _world = null!;
    private SimulationEventBus _eventBus = null!;

    // operator character id → vision source id
    private readonly Dictionary<string, string> _visionSourceIdByOperatorId = new();

    // Global set of NPC character IDs currently within any vision source's range while moving.
    private readonly HashSet<string> _inRange = new();

    // Global set of (siteId, characterId) combos already reported as SpottedAtSite.
    private readonly HashSet<(string siteId, string characterId)> _seenAtSite = new();

    public VisionSystem() { }

    public void Initialize(WorldState world, SimulationEventBus eventBus)
    {
        _world = world;
        _eventBus = eventBus;

        _eventBus.Subscribe<CharacterLocationChangedEvent>(OnCharacterLocationChanged);
    }

    public void Tick(double delta)
    {
        ScanMovingNpcs();

        foreach (var source in _world.VisionSources.Values)
        {
            if (source.Type.CanSeeOperations() || source.Type.CanSeeOccupants())
                ScanSitesForSource(source);
        }
    }

    // ── Vision source lifecycle ───────────────────────────────────────────────

    private void OnCharacterLocationChanged(CharacterLocationChangedEvent evt)
    {
        if (!evt.Character.IsOperator) return;

        if (evt.NewLocation == CharacterLocationType.NavGraph)
            EnsureVisionSource(evt.Character);
        else
            RemoveVisionSource(evt.Character);
    }

    private void EnsureVisionSource(Character character)
    {
        if (_visionSourceIdByOperatorId.ContainsKey(character.Id)) return;

        var sourceId = $"operator:{character.Id}";
        var source = new VisionSource(
            id: sourceId,
            owner: character,
            type: VisionSourceType.OperatorPresence,
            range: character.VisionRange,
            isMapVisible: true);

        source.SetWorldPosition(character.Position.WorldPosition);

        _visionSourceIdByOperatorId[character.Id] = sourceId;
        character.Position.Changed += OnOperatorPositionChanged;

        _world.RegisterVisionSource(source);
    }

    private void RemoveVisionSource(Character character)
    {
        if (!_visionSourceIdByOperatorId.TryGetValue(character.Id, out var sourceId)) return;

        _visionSourceIdByOperatorId.Remove(character.Id);
        character.Position.Changed -= OnOperatorPositionChanged;

        _world.RemoveVisionSource(sourceId);
        RebuildInRange();
    }

    private void OnOperatorPositionChanged(CharacterPosition position)
    {
        foreach (var (operatorId, sourceId) in _visionSourceIdByOperatorId)
        {
            if (!_world.TryGetVisionSource(sourceId, out var source) || source == null) continue;
            if (source.Owner?.Position != position) continue;
            source.SetWorldPosition(position.WorldPosition);
            return;
        }
    }

    // ── Moving NPC scan ───────────────────────────────────────────────────────

    private void ScanMovingNpcs()
    {
        var nowInRange = new HashSet<string>();

        foreach (var source in _world.VisionSources.Values)
        {
            foreach (var candidate in _world.Characters)
            {
                if (candidate.IsOperator || candidate.CurrentMovement == null) continue;
                if (candidate == source.Owner) continue;

                if (source.WorldPosition.DistanceTo(candidate.Position.WorldPosition) <= source.Range)
                    nowInRange.Add(candidate.Id);
            }
        }

        foreach (var characterId in nowInRange)
        {
            if (_inRange.Add(characterId))
            {
                var character = _world.GetCharacter(characterId);
                if (character == null) continue;
                var source = FindBestSourceForCharacter(character);
                if (source == null) continue;
                PublishObservation(source, null, character, null, ObservationType.SpottedMoving);
            }
        }

        _inRange.RemoveWhere(id => !nowInRange.Contains(id));
    }

    private VisionSource? FindBestSourceForCharacter(Character character)
    {
        foreach (var source in _world.VisionSources.Values)
        {
            if (source.WorldPosition.DistanceTo(character.Position.WorldPosition) <= source.Range)
                return source;
        }
        return null;
    }

    private void RebuildInRange()
    {
        _inRange.Clear();
        foreach (var source in _world.VisionSources.Values)
        {
            foreach (var candidate in _world.Characters)
            {
                if (candidate.IsOperator || candidate.CurrentMovement == null) continue;
                if (candidate == source.Owner) continue;
                if (source.WorldPosition.DistanceTo(candidate.Position.WorldPosition) <= source.Range)
                    _inRange.Add(candidate.Id);
            }
        }
    }

    // ── Site scan ─────────────────────────────────────────────────────────────

    private void ScanSitesForSource(VisionSource source)
    {
        foreach (var site in _world.SitesById.Values)
        {
            if (source.WorldPosition.DistanceTo(site.EntryPosition) > source.Range)
                continue;

            var npcsAtSite = new HashSet<Character>();

            if (source.Type.CanSeeOccupants())
                foreach (var occupant in site.Occupants)
                    if (!occupant.IsOperator) npcsAtSite.Add(occupant);

            if (source.Type.CanSeeOperations())
                foreach (var operation in site.ActiveOperations)
                    foreach (var participant in operation.Participants)
                        if (!participant.IsOperator) npcsAtSite.Add(participant);

            foreach (var npc in npcsAtSite)
            {
                var seenKey = (site.Id, npc.Id);
                if (!_seenAtSite.Add(seenKey)) continue;

                Operation? activeOperation = null;
                if (source.Type.CanSeeOperations())
                {
                    foreach (var op in site.ActiveOperations)
                    {
                        if (op.Participants.Contains(npc)) { activeOperation = op; break; }
                    }
                }

                PublishObservation(source, site, npc, activeOperation,
                    ObservationType.SpottedAtSite, ResolveCompliance(source, activeOperation));
            }
        }

        // Clear stale (siteId, characterId) pairs where no source covers the site
        // or the character has left the site.
        _seenAtSite.RemoveWhere(key =>
        {
            if (!_world.SitesById.TryGetValue(key.siteId, out var s)) return true;

            bool anySiteInRange = false;
            foreach (var src in _world.VisionSources.Values)
                if (src.WorldPosition.DistanceTo(s.EntryPosition) <= src.Range)
                { anySiteInRange = true; break; }

            if (!anySiteInRange) return true;

            foreach (var occ in s.Occupants)
                if (occ.Id == key.characterId) return false;

            return true;
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void PublishObservation(
        VisionSource source, Site? site, Character? character, Operation? operation,
        ObservationType observationType, ComplianceType? complianceOverride = null)
    {
        var compliance = complianceOverride ?? ResolveCompliance(source, operation);
        var observation = new Observation(
            id: Guid.NewGuid().ToString(),
            siteId: site?.Id,
            characterId: character?.Id,
            operationId: operation?.Id,
            time: _world.Time,
            observationType: observationType,
            complianceType: compliance,
            siteLabelSnapshot: site?.Label,
            characterLabelSnapshot: character?.DisplayName,
            operationLabelSnapshot: operation?.Label);
        _eventBus.Publish(new ObservationCreatedEvent(observation, _world.Time));
    }

    private static ComplianceType ResolveCompliance(VisionSource source, Operation? operation)
    {
        if (operation == null) return ComplianceType.Compliant;
        if (source.Type.CanDetectNonCompliance()) return operation.ComplianceType;
        return operation.ComplianceType == ComplianceType.NonCompliant
            ? ComplianceType.Suspicious
            : operation.ComplianceType;
    }
}

```

