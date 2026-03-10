using SurveillanceStategodot.scripts.domain.assignment;

namespace SurveillanceStategodot.scripts.domain.interrupt;

/// <summary>
/// A runtime interrupt applied to a single character.
/// Created by any system that wants to override baseline schedule behavior
/// (e.g. RendezvousSystem, SurveillanceSystem).
/// </summary>
public sealed class CharacterInterrupt
{
    public string Id { get; }
    public InterruptType Type { get; }
    public InterruptPriority Priority { get; }
    public InterruptDisposition Disposition { get; }

    /// <summary>The character this interrupt targets.</summary>
    public Character Character { get; }

    /// <summary>
    /// The assignment that should be executed while this interrupt is active.
    /// Must be set before the interrupt is submitted via InterruptRequestedEvent.
    /// </summary>
    public Assignment ReplacementAssignment { get; }

    public bool IsActive { get; set; }

    public CharacterInterrupt(
        string id,
        InterruptType type,
        InterruptPriority priority,
        InterruptDisposition disposition,
        Character character,
        Assignment replacementAssignment)
    {
        Id = id;
        Type = type;
        Priority = priority;
        Disposition = disposition;
        Character = character;
        ReplacementAssignment = replacementAssignment;
    }
}

