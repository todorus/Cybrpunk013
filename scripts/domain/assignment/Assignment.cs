using Godot;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.domain.assignment;

public sealed class Assignment
{
    public string Id { get; }
    public AssignmentKind Kind { get; }
    public Character? Character { get; }

    /// <summary>
    /// For TailCharacter assignments: the NPC being tailed.
    /// </summary>
    public Character? TargetCharacter { get; }

    /// <summary>
    /// The currently active operation for this assignment.
    /// May change over the lifetime of a TailCharacter assignment (watch → pursue → watch …).
    /// Null during movement-only phases.
    /// </summary>
    public Operation? CurrentOperation { get; set; }

    public Movement? CurrentMovement { get; set; }

    public AssignmentCompletionBehavior CompletionBehavior { get; set; } = AssignmentCompletionBehavior.None;
    public AssignmentPhase Phase { get; set; } = AssignmentPhase.Planned;
    public AssignmentSource Source { get; set; } = AssignmentSource.PlayerOrder;

    // Set when Source == Interrupt; links back to the originating CharacterInterrupt.
    public string? InterruptId { get; set; }

    // Optional explicit home/base destination for return logic.
    public Vector3? BaseWorldPosition { get; set; }

    /// <summary>
    /// Standard VisitSite / StakeoutSite assignment constructor.
    /// </summary>
    public Assignment(
        string id,
        Character? character,
        Operation operation,
        Movement? currentMovement,
        AssignmentKind kind = AssignmentKind.VisitSite)
    {
        Id = id;
        Kind = kind;
        Character = character;
        CurrentOperation = operation;
        CurrentMovement = currentMovement;
    }

    /// <summary>
    /// TailCharacter assignment constructor — no initial operation or movement;
    /// AssignmentSystem drives what is created based on target state.
    /// </summary>
    public Assignment(
        string id,
        Character? character,
        Character targetCharacter)
    {
        Id = id;
        Kind = AssignmentKind.TailCharacter;
        Character = character;
        TargetCharacter = targetCharacter;
        CurrentOperation = null;
        CurrentMovement = null;
    }
}

