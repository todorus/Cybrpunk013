namespace SurveillanceStategodot.scripts.domain.system;

public interface ISimulationSystem
{
    void Initialize(WorldState world, SimulationEventBus eventBus);
    void Tick(double delta);
}