using System;
using Godot;
using SurveillanceStategodot.scripts.domain;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.vision;

namespace SurveillanceStategodot.scripts.authoring;

[GlobalClass]
public partial class CharacterResource : Resource
{
    [Export] 
    private string _id = Guid.NewGuid().ToString();
    [Export] 
    private string _displayName = "";

    /// <summary>Stable identifier, matches the runtime Character.Id produced by ToCharacter().</summary>
    public string CharacterId => _id;

    /// <summary>Optional. Assign a ScheduleResource to give this character a baseline routine.</summary>
    [Export] 
    private ScheduleResource Schedule = null;

    /// <summary>
    /// World-units per second. Leave at 0 to use the simulation default.
    /// </summary>
    [Export]
    private float _movementSpeed = 3f;

    /// <summary>
    /// Vision radius in world-units. Leave at 0 to use the simulation default.
    /// </summary>
    [Export]
    private float _visionRange = 3f;

    /// <summary>
    /// Optional 3D scene used by PortraitStudio to render a portrait snapshot.
    /// The scene root should be positioned so it sits correctly at the origin of the SubjectAnchor.
    /// </summary>
    [Export]
    public PackedScene AvatarScene { get; private set; } = null;

    public Character ToCharacter()
    {
        var character = new Character(
            id: _id,
            displayName: _displayName)
        {
            Schedule = Schedule?.ToSchedule(),
            MovementSpeed = _movementSpeed,
            VisionRange = _visionRange
        };

        return character;
    }
}