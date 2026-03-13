namespace SurveillanceStategodot.scripts.domain.vision;

public static class VisionSourceTypeExt
{
    /// <summary>
    /// Whether this source can observe active operations at sites within its range.
    /// Map-level sources (stakeout, watch) can see that an operation is happening
    /// and whether it appears compliant or suspicious, but cannot determine the
    /// specific operation type.
    /// </summary>
    public static bool CanSeeOperations(this VisionSourceType type) => type switch
    {
        VisionSourceType.StakeoutPost  => true,
        VisionSourceType.WatchSite     => true,
        VisionSourceType.MovingOperator => false,
        _ => false
    };

    /// <summary>
    /// Whether this source can observe which characters are occupying a site within its range.
    /// </summary>
    public static bool CanSeeOccupants(this VisionSourceType type) => type switch
    {
        VisionSourceType.StakeoutPost  => true,
        VisionSourceType.WatchSite     => true,
        VisionSourceType.MovingOperator => false,
        _ => false
    };

    /// <summary>
    /// Whether this source can determine that a specific operation is non-compliant
    /// (as opposed to only detecting that something looks suspicious).
    /// Map-level sources can only mark activities as Suspicious; close-inspection
    /// sources would be needed to mark NonCompliant.
    /// </summary>
    public static bool CanDetectNonCompliance(this VisionSourceType type) => type switch
    {
        VisionSourceType.StakeoutPost   => false,
        VisionSourceType.WatchSite      => false,
        VisionSourceType.MovingOperator => false,
        _ => false
    };
}

