using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain.movement;

namespace SurveillanceStategodot.scripts.domain.operation;

public sealed class Operation
{
    public string Id { get; }
    public string Label { get; }
    public OperationState State { get; set; } = OperationState.Planned;

    public List<Character> Participants { get; } = new();
    public Site? SiteContext { get; set; }
    public Movement? MovementContext { get; set; }

    public double StartTime { get; private set; }
    public double Duration { get; }
    public HashSet<OperationObservationTag> ObservationTags { get; } = new();

    public double EndTime => StartTime + Duration;

    public Operation(string id, string label, double duration)
    {
        Id = id;
        Label = label;
        Duration = duration;
    }
    
    public void Start(double worldTime)
    {
        State = OperationState.Active;
        StartTime = worldTime;
    }

    public bool IsComplete(double worldTime) => worldTime >= EndTime;
}