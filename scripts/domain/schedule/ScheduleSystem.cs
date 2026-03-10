using System;
using System.Collections.Generic;
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

    // Characters whose schedule is currently suspended by an interrupt are excluded from
    // idle-detection until the interrupt clears and this set is updated by InterruptSystem.
    private readonly HashSet<string> _suppressedCharacterIds = new();

    public ScheduleSystem(DispatchNav dispatchNav)
    {
        _dispatchNav = dispatchNav;
    }

    public void Initialize(WorldState world, SimulationEventBus eventBus)
    {
        _world = world;
        _eventBus = eventBus;

        _eventBus.Subscribe<AssignmentCompletedEvent>(OnAssignmentCompleted);
    }

    public void Tick(double delta)
    {
        foreach (var character in _world.Characters)
        {
            if (character.Schedule == null || !character.Schedule.HasEntries)
                continue;

            if (_suppressedCharacterIds.Contains(character.Id))
                continue;

            if (character.ActiveInterrupt != null)
                continue;

            if (_world.HasActiveAssignmentForCharacter(character.Id))
                continue;

            IssueNextScheduleAssignment(character);
        }
    }

    /// <summary>Called by InterruptSystem when it takes over a character's schedule slot.</summary>
    public void SuppressCharacter(string characterId) =>
        _suppressedCharacterIds.Add(characterId);

    /// <summary>Called by InterruptSystem when an interrupt clears and baseline should resume.</summary>
    public void UnsuppressCharacter(string characterId) =>
        _suppressedCharacterIds.Remove(characterId);

    private void OnAssignmentCompleted(AssignmentCompletedEvent evt)
    {
        // Nothing extra needed — Tick() will see the character is idle next frame and
        // issue the next schedule entry automatically.
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
            startPosition = character.CurrentSite.GlobalPosition;
        }
        else if (DispatchNavSpawnQueries.TryGetSpawnPoint(_dispatchNav.Graph, site.GlobalPosition, out var spawn))
        {
            startPosition = spawn.Position;
        }
        else
        {
            startPosition = site.GlobalPosition;
        }

        var path = DispatchNavPathfinder.FindPath(_dispatchNav.Graph, startPosition, site.GlobalPosition);

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
            path: path,
            initialPosition: path.StartPosition);

        var operation = new Operation(
            id: Guid.NewGuid().ToString(),
            label: entry.OperationLabel,
            duration: entry.Duration)
        {
            SiteContext = site,
            MovementContext = movement
        };

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



