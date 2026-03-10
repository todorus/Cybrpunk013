using Godot;
using SurveillanceStategodot.scripts.domain.schedule;

namespace SurveillanceStategodot.scripts.authoring;

[GlobalClass]
public partial class ScheduleEntryResource : Resource
{
    /// <summary>Must match the Id of a SiteResource in the scene.</summary>
    [Export] public string SiteId { get; set; } = "";

    [Export] public string OperationLabel { get; set; } = "";

    /// <summary>Dwell / operation duration in world-time seconds.</summary>
    [Export] public double Duration { get; set; } = 10.0;

    public ScheduleEntry ToScheduleEntry()
    {
        return new ScheduleEntry(
            siteId: SiteId,
            operationLabel: OperationLabel,
            duration: Duration);
    }
}

