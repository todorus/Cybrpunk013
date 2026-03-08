using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.movement;

public sealed class MovementSystem : ISimulationSystem
{
    private WorldState _world = null!;
    private SimulationEventBus _eventBus = null!;

    public void Initialize(WorldState world, SimulationEventBus eventBus)
    {
        _world = world;
        _eventBus = eventBus;
    }

    public void Tick(double delta)
    {
        if (_world.ActiveMovements.Count == 0)
            return;

        var arrived = new List<Movement>();

        foreach (var movement in _world.ActiveMovements)
        {
            if (_world.Time >= movement.EndTime)
            {
                arrived.Add(movement);
            }
        }

        foreach (var movement in arrived)
        {
            movement.Character.CurrentMovement = null;
            movement.Character.CurrentSite = movement.Destination;

            movement.Destination.Occupants.Add(movement.Character);

            _world.ActiveMovements.Remove(movement);
            _eventBus.Publish(new MovementArrivedEvent(movement, _world.Time));
        }
    }

    public Movement StartMovement(Character character, Site destination, double travelDuration)
    {
        if (character.CurrentSite == null)
            throw new System.InvalidOperationException("Character must be at a site before moving.");

        var origin = character.CurrentSite;
        origin.Occupants.Remove(character);

        var movement = new Movement(
            id: System.Guid.NewGuid().ToString(),
            character: character,
            origin: origin,
            destination: destination,
            startTime: _world.Time,
            endTime: _world.Time + travelDuration);

        character.CurrentSite = null;
        character.CurrentMovement = movement;

        _world.ActiveMovements.Add(movement);
        _eventBus.Publish(new MovementStartedEvent(movement, _world.Time));

        return movement;
    }
}