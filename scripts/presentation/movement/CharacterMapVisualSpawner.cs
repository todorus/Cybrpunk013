using Godot;
using SurveillanceStategodot.scripts.domain;

namespace SurveillanceStategodot.scripts.presentation.movement;

public partial class CharacterMapVisualSpawner : Node
{
    [Export] private PackedScene _characterMapVisualScene = null!;
    [Export] private Node3D _visualRoot = null!;

    public CharacterMapVisual SpawnForCharacter(Character character)
    {
        var visual = _characterMapVisualScene.Instantiate<CharacterMapVisual>();
        _visualRoot.AddChild(visual);
        visual.SetCharacter(character);
        return visual;
    }
}