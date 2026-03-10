using Godot;
using SurveillanceStategodot.scripts.domain.plot;

namespace SurveillanceStategodot.scripts.authoring;

public class PlotResource
{
    [Export] private string _id = "";
    [Export] private string _label = "";
    [Export] private CharacterResource[] _characters = [];
    
    public Plot ToPlot()
    {
        var plot = new Plot(
            id: _id,
            label: _label);
        
        foreach (var characterResource in _characters)
        {
            plot.Characters.Add(characterResource.ToCharacter());
        }

        return plot;
    }
}