using System;
using Godot;
using SurveillanceStategodot.scripts.domain.assignment;

namespace SurveillanceStategodot.scripts.authoring;

[GlobalClass]
public partial class OptionResource : Resource
{
    [Export]
    private string _id = Guid.NewGuid().ToString();
    [Export]
    private string _label;
    [Export]
    private double _duration;
    
    public Option ToOption()
    {
        return new Option
        (
            id: _id,
            label: _label,
            duration: _duration
        );
    }
}