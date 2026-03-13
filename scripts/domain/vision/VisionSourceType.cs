namespace SurveillanceStategodot.scripts.domain.vision;

public enum VisionSourceType
{
    /// <summary>
    /// Unified operator map-level vision source. Active while the operator is on
    /// the nav graph (CurrentSite == null). Can see moving NPCs, site occupants,
    /// and operations within range. Removed when the operator enters a site;
    /// recreated when they exit.
    /// </summary>
    OperatorPresence
}