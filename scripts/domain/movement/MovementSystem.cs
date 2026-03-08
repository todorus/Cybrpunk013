using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.movement;

public sealed class MovementSystem : ISimulationSystem
{
    private WorldState _world = null!;
    private SimulationEventBus _eventBus = null!;
    
    private readonly List<Movement> _activeMovements = new();

    private static float Speed = 3f;
    
    public void Initialize(WorldState world, SimulationEventBus eventBus)
    {
        _world = world;
        _eventBus = eventBus;
        
        _eventBus.Subscribe<MovementStartedEvent>(OnMovementStarted);
    }


    public void Tick(double delta)
    {
        for (int i = _activeMovements.Count - 1; i >= 0; i--)
        {
            var movement = _activeMovements[i];
            movement.Advance(Speed * (float)delta);

            if (movement.HasArrived)
            {
                _activeMovements.RemoveAt(i);
                // occupancy updates, site arrival logic, etc.
            }
        }
    }

    private void OnMovementStarted(MovementStartedEvent evt)
    {
        _activeMovements.Add(evt.Movement);
    }
}