namespace SurveillanceStategodot.scripts.domain.movement;

public enum MovementMode
{
    /// <summary>Normal movement along a pre-computed static path.</summary>
    StaticPath,

    /// <summary>
    /// Pursuit mode: the path is periodically recomputed toward a moving target.
    /// HasArrived is never set by the path end alone; AssignmentSystem decides when pursuit ends.
    /// </summary>
    Pursuit
}

