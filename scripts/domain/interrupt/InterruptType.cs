namespace SurveillanceStategodot.scripts.domain.interrupt;

/// <summary>
/// Describes the kind of reactive behavior that caused the interrupt.
/// Extend freely as new NPC reactions are added.
/// </summary>
public enum InterruptType
{
    // Surveillance / player-induced
    TailOpportunity,
    SuspiciousIntercept,
    TailAtRisk,
    ResistanceEncounter,
    ReactToSurveillance,

    // Coordination
    Rendezvous,

    // Operational security
    BugSweep,
    MoveStash,

    // Escape / evasion
    Flee,
    Relocate,

    // Lying low
    LayLow
}

