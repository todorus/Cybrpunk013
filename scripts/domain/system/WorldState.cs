using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain.communication;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.domain.system;

public sealed class WorldState
{
    public double Time { get; private set; }

    public List<Character> Characters { get; } = new();
    public List<Site> Sites { get; } = new();
    public List<Movement> ActiveMovements { get; } = new();
    public List<Operation> ActiveOperations { get; } = new();
    public List<Communication> Communications { get; } = new();
    public List<Intercept> Intercepts { get; } = new();
    // public List<Interrupt> PendingInterrupts { get; } = new();

    public void AdvanceTime(double delta)
    {
        Time += delta;
    }
}