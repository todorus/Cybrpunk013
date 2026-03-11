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
