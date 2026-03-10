namespace SurveillanceStategodot.scripts.domain.interrupt;

public enum InterruptDisposition
{
    /// <summary>
    /// The current baseline assignment is saved and will be resumed when the interrupt clears.
    /// </summary>
    Suspend,

    /// <summary>
    /// The current baseline assignment is cancelled outright; schedule resumes from next entry.
    /// </summary>
    Replace
}

