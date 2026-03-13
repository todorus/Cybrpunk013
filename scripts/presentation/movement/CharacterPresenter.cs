using System.Collections.Generic;
using Godot;
using SurveillanceStategodot.scripts.domain;
using SurveillanceStategodot.scripts.interaction;

namespace SurveillanceStategodot.scripts.presentation.movement;

/// <summary>
/// Spawns and manages one CharacterMapVisual per character on the map.
/// A visual is created the first time a character transitions to NavGraph.
/// Hidden when the character is at a Site; shown when on the NavGraph.
/// For operators, destroyed when they return to Base.
/// For NPCs, the visual persists across schedule cycles.
/// </summary>
public partial class CharacterPresenter : Node
{
    [Export] private CharacterMapVisualSpawner _spawner = null!;
    [Export] private SimulationController _simulationController = null!;

    private readonly Dictionary<string, CharacterMapVisual> _visualsByCharacterId = new();

    public override void _Ready()
    {
        _simulationController.EventBus.Subscribe<CharacterLocationChangedEvent>(OnCharacterLocationChanged);
    }

    public override void _ExitTree()
    {
        if (_simulationController?.EventBus != null)
        {
            _simulationController.EventBus.Unsubscribe<CharacterLocationChangedEvent>(OnCharacterLocationChanged);
        }
    }

    private void OnCharacterLocationChanged(CharacterLocationChangedEvent evt)
    {
        var character = evt.Character;

        switch (evt.NewLocation)
        {
            case CharacterLocationType.NavGraph:
                // Ensure a visual exists (first time on the map).
                if (!_visualsByCharacterId.ContainsKey(character.Id))
                {
                    var visual = _spawner.SpawnForCharacter(character);
                    _visualsByCharacterId[character.Id] = visual;
                }
                else
                {
                    _visualsByCharacterId[character.Id].Visible = true;
                }
                break;

            case CharacterLocationType.Site:
                if (_visualsByCharacterId.TryGetValue(character.Id, out var siteVisual))
                    siteVisual.Visible = false;
                break;

            case CharacterLocationType.Base:
                // Operators leave the map when they return to base.
                if (character.IsOperator && _visualsByCharacterId.TryGetValue(character.Id, out var baseVisual))
                {
                    _visualsByCharacterId.Remove(character.Id);
                    baseVisual.QueueFree();
                }
                break;
        }
    }
}