using System.Collections.Generic;
using Godot;
using SurveillanceStategodot.scripts.domain.assignment;
using SurveillanceStategodot.scripts.domain.interrupt;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.domain.plot;
using SurveillanceStategodot.scripts.domain.schedule;
using SurveillanceStategodot.scripts.domain.system;
using SurveillanceStategodot.scripts.navigation.authoring;

namespace SurveillanceStategodot.scripts.interaction;

public partial class SimulationController : Node
{
    [Signal] 
    public delegate void SimulationInitializedEventHandler(SimulationController controller);
    
    [Export] private DispatchNav _dispatchNav = null!;

    public WorldState World { get; private set; } = null!;
    public SimulationEventBus EventBus { get; private set; } = null!;

    private readonly List<ISimulationSystem> _systems = new();

    public override void _Ready()
    {
        World = new WorldState();
        EventBus = new SimulationEventBus();

        _systems.Add(new PlotSystem());
        // Order matters: Schedule and Interrupt decide what to issue,
        // then AssignmentSystem executes the resulting events in the same frame.
        _systems.Add(new ScheduleSystem(_dispatchNav));
        _systems.Add(new InterruptSystem());
        _systems.Add(new AssignmentSystem(_dispatchNav));
        _systems.Add(new MovementSystem());
        _systems.Add(new OperationSystem());

        foreach (var system in _systems)
        {
            system.Initialize(World, EventBus);
        }
        
        EmitSignalSimulationInitialized(this);
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