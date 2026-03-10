using System;
using Godot;
using SurveillanceStategodot.scripts.domain;

namespace SurveillanceStategodot.scripts.authoring;

[GlobalClass]
public partial class CharacterResource : Resource
{
    [Export] 
    private string _id = Guid.NewGuid().ToString();
    [Export] 
    private string _displayName = "";

    /// <summary>Optional. Assign a ScheduleResource to give this character a baseline routine.</summary>
    [Export] 
    private ScheduleResource Schedule = null;

    public Character ToCharacter()
    {
        var character = new Character(
            id: _id,
            displayName: _displayName)
        {
            Schedule = Schedule?.ToSchedule()
        };

        return character;
    }
}