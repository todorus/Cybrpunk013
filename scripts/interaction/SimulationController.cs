using System.Collections.Generic;
using Godot;
using SurveillanceStategodot.scripts.domain.assignment;
using SurveillanceStategodot.scripts.domain.interrupt;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.observation;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.domain.plot;
using SurveillanceStategodot.scripts.domain.schedule;
using SurveillanceStategodot.scripts.domain.system;
using SurveillanceStategodot.scripts.domain.vision;
using SurveillanceStategodot.scripts.navigation.authoring;

namespace SurveillanceStategodot.scripts.interaction;

public partial class SimulationController : Node
{
    [Signal] 
    public delegate void SimulationInitializedEventHandler(SimulationController controller);
    
    [Signal]
    public delegate void OperaterVisionRangeChangedEventHandler(float range);
    
    [Export] private float _operatorVisionRange = 3f;
    
    [Export] private DispatchNav _dispatchNav = null!;

    public WorldState World { get; private set; } = null!;
    public SimulationEventBus EventBus { get; private set; } = null!;

    private readonly List<ISimulationSystem> _systems = new();

    public override void _Ready()
    {
        EmitSignalOperaterVisionRangeChanged(_operatorVisionRange);
        
        World = new WorldState();
        EventBus = new SimulationEventBus();

        _systems.Add(new PlotSystem());
        
        // Order matters: Schedule and Interrupt decide what to issue,
        // then AssignmentSystem executes the resulting events in the same frame.
        _systems.Add(new ScheduleSystem(_dispatchNav));
        _systems.Add(new InterruptSystem());
        _systems.Add(new AssignmentSystem(_dispatchNav));
        
        _systems.Add(new MovementSystem(_dispatchNav));
        _systems.Add(new OperationSystem());
        
        _systems.Add(new VisionSystem());
        _systems.Add(new ObservationLogSystem());

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