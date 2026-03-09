using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.domain.assignment;

public sealed class Assignment
{
    public string Id { get; }
    public Character? Character { get; }
    public Operation Operation { get; }
    public Movement Movement { get; }
    public AssignmentState State { get; set; } = AssignmentState.Planned;

    public Assignment(string id, Character? character, Operation operation, Movement movement)
    {
        Id = id;
        Character = character;
        Operation = operation;
        Movement = movement;
    }
}