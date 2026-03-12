namespace SurveillanceStategodot.scripts.domain.vision;

public enum VisionSourceType
{
    MovingOperator,
    StakeoutPost,
    /// <summary>Fixed-position sensor created during a TailCharacter WatchingTargetSite phase.</summary>
    WatchSite
}