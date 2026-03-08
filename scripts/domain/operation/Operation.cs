using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain.movement;

namespace SurveillanceStategodot.scripts.domain.operation;

public sealed class Operation
{
    public string Id { get; }
    public OperationType Type { get; }
    public OperationState State { get; set; } = OperationState.Planned;

    public List<Character> Participants { get; } = new();
    public Site? SiteContext { get; set; }
    public Movement? MovementContext { get; set; }

    public double StartTime { get; }
    public double Duration { get; }
    public HashSet<OperationObservationTag> ObservationTags { get; } = new();

    public double EndTime => StartTime + Duration;

    public Operation(string id, OperationType type, double startTime, double duration)
    {
        Id = id;
        Type = type;
        StartTime = startTime;
        Duration = duration;
    }

    public bool IsComplete(double worldTime) => worldTime >= EndTime;
}