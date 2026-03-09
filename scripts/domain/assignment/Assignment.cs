using Godot;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.domain.assignment;

public sealed class Assignment
{
    public string Id { get; }
    public Character? Character { get; }

    public Operation Operation { get; }
    public Movement? CurrentMovement { get; set; }

    public AssignmentCompletionBehavior CompletionBehavior { get; set; } = AssignmentCompletionBehavior.None;
    public AssignmentPhase Phase { get; set; } = AssignmentPhase.OutboundMovement;

    // Optional explicit home/base destination for return logic.
    public Vector3? BaseWorldPosition { get; set; }

    public Assignment(
        string id,
        Character? character,
        Operation operation,
        Movement? currentMovement)
    {
        Id = id;
        Character = character;
        Operation = operation;
        CurrentMovement = currentMovement;
    }
}