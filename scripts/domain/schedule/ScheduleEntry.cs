namespace SurveillanceStategodot.scripts.domain.schedule;

/// <summary>
/// A single step in an NPC's baseline routine.
/// SiteId is resolved to a runtime Site via WorldState at execution time.
/// </summary>
public sealed class ScheduleEntry
{
    /// <summary>Runtime site ID to look up in WorldState.</summary>
    public string SiteId { get; }

    /// <summary>Human-readable label for the operation performed at the site.</summary>
    public string OperationLabel { get; }

    /// <summary>How long the NPC dwells / operates at the site (world-time seconds).</summary>
    public double Duration { get; }

    public ScheduleEntry(string siteId, string operationLabel, double duration)
    {
        SiteId = siteId;
        OperationLabel = operationLabel;
        Duration = duration;
    }
}

