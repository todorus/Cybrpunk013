using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain.communication;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.domain.schedule;

namespace SurveillanceStategodot.scripts.domain;

public sealed class Character
{
    public string Id { get; }
    public string DisplayName { get; set; }
    public SuspicionLevel SuspicionLevel { get; set; } = SuspicionLevel.None;

    public Schedule? Schedule { get; set; }
    public Site? CurrentSite { get; set; }
    public Movement? CurrentMovement { get; set; }

    public List<Interceptor> Interceptors { get; } = new();

    public Character(string id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }
}