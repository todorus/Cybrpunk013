using System.Collections.Generic;
using Godot;
using SurveillanceStategodot.scripts.domain.assignment;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.interaction;

public partial class SimulationController : Node
{
    public WorldState World { get; private set; } = null!;
    public SimulationEventBus EventBus { get; private set; } = null!;

    private readonly List<ISimulationSystem> _systems = new();

    public override void _Ready()
    {
        World = new WorldState();
        EventBus = new SimulationEventBus();

        _systems.Add(new AssignmentSystem());
        _systems.Add(new MovementSystem());
        _systems.Add(new OperationSystem());

        foreach (var system in _systems)
        {
            system.Initialize(World, EventBus);
        }
    }

    public override void _Process(double delta)
    {
        World.AdvanceTime(delta);

        foreach (var system in _systems)
        {
            system.Tick(delta);
        }
    }
}