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

    private const float Speed = 3f;

    // Pursuit repathing interval in world-time seconds.
    private const double RepathInterval = 1.0;
    private readonly Dictionary<string, double> _nextRepathTime = new();

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

            // Pursuit: repath periodically toward the target's current position.
            if (movement.Mode == MovementMode.Pursuit)
            {
                TryRepathPursuit(movement);
            }

            AdvanceMovement(movement, Speed * (float)delta);

            if (!movement.HasArrived)
                continue;

            _activeMovements.RemoveAt(i);
            _nextRepathTime.Remove(movement.Id);

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

    private void TryRepathPursuit(Movement movement)
    {
        if (!_nextRepathTime.TryGetValue(movement.Id, out var nextRepath))
        {
            _nextRepathTime[movement.Id] = _world.Time + RepathInterval;
            return;
        }

        if (_world.Time < nextRepath)
            return;

        _nextRepathTime[movement.Id] = _world.Time + RepathInterval;

        var target = movement.TargetCharacter;
        if (target == null)
            return;

        // Determine target world position from the authoritative position component.
        Vector3 targetPos;
        if (target.CurrentSite != null)
            targetPos = target.CurrentSite.EntryPosition;
        else
            targetPos = target.Position.WorldPosition;

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

        if (movement.Character != null)
        {
            movement.Character.CurrentMovement = null;
        }
    }
}
