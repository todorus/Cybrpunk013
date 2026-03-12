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

            movement.Advance(Speed * (float)delta);

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

        // Determine target world position: prefer live movement position, then site entry.
        Vector3 targetPos;
        if (target.CurrentMovement != null)
            targetPos = target.CurrentMovement.CurrentWorldPosition;
        else if (target.CurrentSite != null)
            targetPos = target.CurrentSite.EntryPosition;
        else
            return; // Target position unknown; keep current path.

        var newPath = DispatchNavPathfinder.FindPath(
            _dispatchNav.Graph,
            movement.CurrentWorldPosition,
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

