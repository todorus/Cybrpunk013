namespace SurveillanceStategodot.scripts.domain.vision;

public static class VisionSourceTypeExt
{
    /// <summary>
    /// Whether this source can observe active operations at sites within its range.
    /// Map-level: can detect that something is happening and whether it looks suspicious,
    /// but cannot determine the specific operation type.
    /// </summary>
    public static bool CanSeeOperations(this VisionSourceType type) => type switch
    {
        VisionSourceType.OperatorPresence => true,
        _ => false
    };

    /// <summary>
    /// Whether this source can observe which characters are occupying a site within its range.
    /// </summary>
    public static bool CanSeeOccupants(this VisionSourceType type) => type switch
    {
        VisionSourceType.OperatorPresence => true,
        _ => false
    };

    /// <summary>
    /// Whether this source can determine that a specific operation is non-compliant.
    /// Map-level sources can only mark activities as Suspicious; a close-inspection
    /// source would be needed to confirm NonCompliant.
    /// </summary>
    public static bool CanDetectNonCompliance(this VisionSourceType type) => type switch
    {
        VisionSourceType.OperatorPresence => false,
        _ => false
    };
}
