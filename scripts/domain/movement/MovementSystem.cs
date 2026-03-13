using System.Collections.Generic;
using Godot;
using SurveillanceStategodot.scripts.domain.assignment;
using SurveillanceStategodot.scripts.domain.operation;
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

    // Pursuit repathing interval in world-time seconds.
    private const double RepathInterval = 1.0;
    private readonly Dictionary<string, double> _nextRepathTime = new();

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

            // Pursuit: repath periodically, or immediately if the path end is reached.
            if (movement.Mode == MovementMode.Pursuit)
            {
                if (PursuitPathExhausted(movement))
                    RepathPursuit(movement);
                else
                    TryRepathPursuit(movement);
            }

            var speed = ResolveSpeed(movement);
            AdvanceMovement(movement, speed * (float)delta);

            if (!movement.HasArrived)
                continue;

            _activeMovements.RemoveAt(i);
            _nextRepathTime.Remove(movement.Id);
            _pursuitZone.Remove(movement.Id);

            if (movement.Character != null)
            {
                movement.Character.CurrentMovement = null;
                movement.Character.CurrentSite = movement.Destination;

                if (movement.Destination != null)
                {
                    movement.Destination.AddOccupant(movement.Character);

                    // Update authoritative position to the site entry point.
                    movement.Character.Position.Set(movement.Destination.EntryPosition);

                    _eventBus.Publish(new CharacterEnteredSiteEvent(
                        movement.Character,
                        movement.Destination,
                        CurrentOperation: null,
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
    /// Returns true when the pursuit movement has walked to the end of its
    /// current path — meaning the operator has caught up to where the target
    /// was last seen and needs a fresh path immediately.
    /// </summary>
    private static bool PursuitPathExhausted(Movement movement)
    {
        if (!movement.Path.IsValid || movement.Path.WorldPoints.Count < 2)
            return false;
        return movement.SegmentIndex >= movement.Path.WorldPoints.Count - 1;
    }

    /// <summary>Repath only when the scheduled interval has elapsed.</summary>
    private void TryRepathPursuit(Movement movement)
    {
        if (!_nextRepathTime.TryGetValue(movement.Id, out var nextRepath))
        {
            _nextRepathTime[movement.Id] = _world.Time + RepathInterval;
            return;
        }

        if (_world.Time < nextRepath)
            return;

        RepathPursuit(movement);
    }

    /// <summary>Unconditionally repaths pursuit and resets the repath timer.</summary>
    private void RepathPursuit(Movement movement)
    {
        _nextRepathTime[movement.Id] = _world.Time + RepathInterval;

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

                _eventBus.Publish(new CharacterExitedSiteEvent(
                    character,
                    previousSite,
                    CurrentOperation: null,
                    _world.Time));
            }

            character.CurrentSite = null;
        }
    }

    private void OnAssignmentCancelled(AssignmentCancelledEvent evt)
    {
        var movement = evt.Assignment.CurrentMovement;
        if (movement == null)
            return;

        _activeMovements.Remove(movement);
        _nextRepathTime.Remove(movement.Id);
        _pursuitZone.Remove(movement.Id);

        if (movement.Character != null)
        {
            movement.Character.CurrentMovement = null;
        }
    }
}
