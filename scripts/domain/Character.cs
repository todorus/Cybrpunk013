using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain.assignment;
using SurveillanceStategodot.scripts.domain.communication;
using SurveillanceStategodot.scripts.domain.interrupt;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.domain.schedule;

namespace SurveillanceStategodot.scripts.domain;

public sealed class Character
{
    public string Id { get; }
    public string DisplayName { get; set; }
    public bool IsOperator { get; set; }
    
    public SuspicionLevel SuspicionLevel { get; set; } = SuspicionLevel.None;

    public Schedule? Schedule { get; set; }
    public Site? CurrentSite { get; set; }
    public Movement? CurrentMovement { get; set; }

    /// <summary>The currently active interrupt, if any.</summary>
    public CharacterInterrupt? ActiveInterrupt { get; set; }

    /// <summary>
    /// A baseline schedule assignment that was suspended by an interrupt with Suspend disposition.
    /// Restored by InterruptSystem when the interrupt clears.
    /// </summary>
    public Assignment? SuspendedAssignment { get; set; }

    public List<Interceptor> Interceptors { get; } = new();

    public Character(string id, string displayName, bool isOperator = false)
    {
        Id = id;
        DisplayName = displayName;
        IsOperator = isOperator;
    }
}