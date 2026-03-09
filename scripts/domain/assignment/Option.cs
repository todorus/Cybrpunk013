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
    
    public Operation ToOperation()
    {
        return new Operation
        (
            id: Id,
            label: Label,
            duration: Duration
        );
    }
    
}