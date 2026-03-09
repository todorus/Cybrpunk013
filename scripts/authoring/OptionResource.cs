using System;
using Godot;
using SurveillanceStategodot.scripts.domain.assignment;
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.authoring;

[GlobalClass]
public partial class OperationResource : Resource
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