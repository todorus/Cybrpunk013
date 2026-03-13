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
    public ComplianceType ComplianceType { get; }

    public Option(string id, string label, double duration, OperationVisionType visionType = OperationVisionType.None, ComplianceType complianceType = ComplianceType.Compliant)
    {
        Id = id;
        Label = label;
        Duration = duration;
        VisionType = visionType;
        ComplianceType = complianceType;
    }

    public Operation ToOperation(Movement movement, Site site)
    {
        return new Operation(
            id: Guid.NewGuid().ToString(),
            label: Label,
            duration: Duration,
            visionType: VisionType,
            complianceType: ComplianceType)
        {
            SiteContext = site,
            MovementContext = movement
        };
    }
}