using Godot;
using SurveillanceStategodot.scripts.domain;

namespace SurveillanceStategodot.scripts.authoring;

[GlobalClass]
public partial class CharacterResource : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";

    /// <summary>Optional. Assign a ScheduleResource to give this character a baseline routine.</summary>
    [Export] public ScheduleResource Schedule { get; set; } = null;

    public Character ToCharacter()
    {
        var character = new Character(
            id: Id,
            displayName: DisplayName);

        if (Schedule != null)
            character.Schedule = Schedule.ToSchedule();

        return character;
    }
}