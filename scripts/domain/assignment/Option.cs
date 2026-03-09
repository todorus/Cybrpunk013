using System;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.domain.assignment;

public class Option
{
    public string Id { get; }
    public string Label { get; }
    public double Duration { get; }

    public Option(string id, string label, double duration)
    {
        Id = id;
        Label = label;
        Duration = duration;
    }
    
    public Operation ToOperation(Movement movement, Site site)
    {
        return new Operation
        (
            id: Guid.NewGuid().ToString(),
            label: Label,
            duration: Duration
        )
        {
            SiteContext = site,
            MovementContext = movement
        };
    }
    
}