using System.Collections.Generic;
using Godot;
using SurveillanceStategodot.scripts.domain;
using SurveillanceStategodot.scripts.domain.assignment;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.interaction;

namespace SurveillanceStategodot.scripts.presentation.movement;

/// <summary>
/// Spawns and manages one CharacterMapVisual per character on the map.
/// A visual is created on the first MovementStartedEvent for that character.
/// Hidden when the character is inside a site; shown again when they leave.
/// For operators, it is destroyed when their assignment completes (they leave the map).
/// For NPCs, it persists across schedule cycles.
/// </summary>
public partial class CharacterPresenter : Node
{
    [Export] private CharacterMapVisualSpawner _spawner = null!;
    [Export] private SimulationController _simulationController = null!;

    private readonly Dictionary<string, CharacterMapVisual> _visualsByCharacterId = new();

    public override void _Ready()
    {
        _simulationController.EventBus.Subscribe<MovementStartedEvent>(OnMovementStarted);
        _simulationController.EventBus.Subscribe<AssignmentCompletedEvent>(OnAssignmentCompleted);
        _simulationController.EventBus.Subscribe<CharacterEnteredSiteEvent>(OnCharacterEnteredSite);
        _simulationController.EventBus.Subscribe<CharacterExitedSiteEvent>(OnCharacterExitedSite);
    }

    public override void _ExitTree()
    {
        if (_simulationController?.EventBus != null)
        {
            _simulationController.EventBus.Unsubscribe<MovementStartedEvent>(OnMovementStarted);
            _simulationController.EventBus.Unsubscribe<AssignmentCompletedEvent>(OnAssignmentCompleted);
            _simulationController.EventBus.Unsubscribe<CharacterEnteredSiteEvent>(OnCharacterEnteredSite);
            _simulationController.EventBus.Unsubscribe<CharacterExitedSiteEvent>(OnCharacterExitedSite);
        }
    }

    private void OnMovementStarted(MovementStartedEvent evt)
    {
        var character = evt.Movement.Character;
        if (character == null) return;

        // Only one visual per character.
        if (_visualsByCharacterId.ContainsKey(character.Id)) return;

        var visual = _spawner.SpawnForCharacter(character);
        _visualsByCharacterId[character.Id] = visual;
    }

    private void OnCharacterEnteredSite(CharacterEnteredSiteEvent evt)
    {
        if (_visualsByCharacterId.TryGetValue(evt.Character.Id, out var visual))
            visual.Visible = false;
    }

    private void OnCharacterExitedSite(CharacterExitedSiteEvent evt)
    {
        if (_visualsByCharacterId.TryGetValue(evt.Character.Id, out var visual))
            visual.Visible = true;
    }

    private void OnAssignmentCompleted(AssignmentCompletedEvent evt)
    {
        var character = evt.Assignment.Character;
        if (character == null) return;

        // Only remove operator visuals when they have no more active assignments.
        // NPC visuals persist — they will get new schedule assignments.
        if (!character.IsOperator) return;

        // Check if the operator still has an active assignment (e.g. interrupted and restored).
        if (_simulationController.World.HasActiveAssignmentForCharacter(character.Id)) return;

        if (_visualsByCharacterId.TryGetValue(character.Id, out var visual))
        {
            _visualsByCharacterId.Remove(character.Id);
            visual.QueueFree();
        }
    }
}