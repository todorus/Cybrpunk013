namespace SurveillanceStategodot.scripts.domain.vision;

public enum VisionSourceType
{
    /// <summary>Follows a moving operator. Map-level only: spots moving NPCs.</summary>
    MovingOperator,
    /// <summary>Fixed sensor placed by a stakeout operation. Can observe site operations and occupants at map level.</summary>
    StakeoutPost,
    /// <summary>Fixed-position sensor created during a TailCharacter HoldingPosition phase. Same map-level capability as StakeoutPost.</summary>
    WatchSite
}