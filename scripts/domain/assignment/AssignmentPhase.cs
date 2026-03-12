namespace SurveillanceStategodot.scripts.domain.assignment;

public enum AssignmentPhase
{
    Planned,
    OutboundMovement,
    OnSiteOperation,
    ReturnMovement,
    // Tail-specific phases
    WatchingTargetSite,
    PursuingTarget,
    LostTarget,
    // Terminal phases
    Completed,
    Cancelled,
    Failed
}