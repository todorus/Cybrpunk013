using System;
using System.Collections.Generic;
using Godot;
using SurveillanceStategodot.scripts.domain.plot;

namespace SurveillanceStategodot.scripts.authoring;

[GlobalClass]
public partial class PlotResource : Resource
{
    [Export] private string _id = Guid.NewGuid().ToString();
    [Export] private string _label = "";
    [Export] private CharacterResource[] _characters = [];

    /// <summary>Exposes the authored character resources for indexing by ResourceRegistry.</summary>
    public IReadOnlyList<CharacterResource> Characters => _characters;
    
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