using System.Collections.Generic;

namespace SurveillanceStategodot.scripts.domain.plot;

public class Plot
{
    public bool Initialized = false;
    
    public string Id { get; }
    public string Label { get; }

    public List<Character> Characters { get; } = new();

    public Plot(string id, string label)
    {
        Id = id;
        Label = label;
    }
}