namespace SurveillanceStategodot.scripts.domain.assignment;

public enum AssignmentPhase
{
    Planned,
    OutboundMovement,
    OnSiteOperation,
    ReturnMovement,
    // Tail-specific phases
    HoldingPosition,  // Operator is stationary, watching the target's last known position.
    PursuingTarget,   // Operator is moving toward the target.
    LostTarget,       // Target lost — operator holds at last known position.
    // Terminal phases
    Completed,
    Cancelled,
    Failed
}