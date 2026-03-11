using System;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.domain.assignment;

public class Option
{
    public string Id { get; }
    public string Label { get; }
    public double Duration { get; }
    public OperationVisionType VisionType { get; }

    public Option(string id, string label, double duration, OperationVisionType visionType = OperationVisionType.None)
    {
        Id = id;
        Label = label;
        Duration = duration;
        VisionType = visionType;
    }
    
    public Operation ToOperation(Movement movement, Site site)
    {
        return new Operation
        (
            id: Guid.NewGuid().ToString(),
            label: Label,
            duration: Duration,
            visionType: VisionType
        )
        {
            SiteContext = site,
            MovementContext = movement
        };
    }
}