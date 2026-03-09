using Godot;
using SurveillanceStategodot.scripts.domain;

namespace SurveillanceStategodot.scripts.authoring;

[GlobalClass]
public partial class CharacterResource : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";

    public Character ToCharacter()
    {
        return new Character
        (
            id: Id,
            displayName: DisplayName
        );
    }
}