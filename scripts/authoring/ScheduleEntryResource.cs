using Godot;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.domain.schedule;

namespace SurveillanceStategodot.scripts.authoring;

[GlobalClass]
public partial class ScheduleEntryResource : Resource
{
    /// <summary>Must match the Id of a SiteResource in the scene.</summary>
    [Export] 
    private SiteResource _site;

    [Export] 
    private string OperationLabel { get; set; } = "";
    
    [Export]
    private ComplianceType ComplianceType { get; set; }

    /// <summary>Dwell / operation duration in world-time seconds.</summary>
    [Export] public double Duration { get; set; } = 10.0;

    public ScheduleEntry ToScheduleEntry()
    {
        return new ScheduleEntry(
            siteId: _site.Id,
            operationLabel: OperationLabel,
            duration: Duration,
            complianceType: ComplianceType);
    }
}

