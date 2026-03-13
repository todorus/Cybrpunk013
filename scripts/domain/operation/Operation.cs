using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain.movement;

namespace SurveillanceStategodot.scripts.domain.operation;

public sealed class Operation
{
    public string Id { get; }
    public string Label { get; }
    public OperationState State { get; set; } = OperationState.Planned;

    public ComplianceType ComplianceType { get; }

    public List<Character> Participants { get; } = new();
    public Site? SiteContext { get; set; }
    public Movement? MovementContext { get; set; }

    public double StartTime { get; private set; }
    public double Duration { get; }
    public OperationVisionType VisionType { get; }
    public HashSet<OperationObservationTag> ObservationTags { get; } = new();

    public double EndTime => StartTime + Duration;

    public Operation(string id, string label, double duration, OperationVisionType visionType = OperationVisionType.None, ComplianceType complianceType = ComplianceType.Compliant)
    {
        Id = id;
        Label = label;
        Duration = duration;
        VisionType = visionType;
        ComplianceType = complianceType;
    }
    
    public void Start(double worldTime)
    {
        State = OperationState.Active;
        StartTime = worldTime;
    }

    public bool IsComplete(double worldTime) => worldTime >= EndTime;
}