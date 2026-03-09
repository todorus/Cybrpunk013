using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.domain.assignment;

public struct Assignment
{
    public Character Character { get; }
    public Operation Operation { get; }
    public Movement Movement { get; }
}