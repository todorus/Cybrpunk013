using System;
using Godot;
using SurveillanceStategodot.scripts.authoring;
using SurveillanceStategodot.scripts.domain;
using SurveillanceStategodot.scripts.domain.assignment;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.navigation.authoring;
using SurveillanceStategodot.scripts.navigation.query;
using SurveillanceStategodot.scripts.presentation.sites;

namespace SurveillanceStategodot.scripts.interaction;

public partial class CityscapeClickHandler : Node
{
    [Export] private DispatchNav _dispatchNav = null!;
    [Export] private Node3D _spawnWorldPosition = null!;
    [Export] private Node3D _operatorBaseWorldPosition = null!;
    [Export] private SimulationController _simulationController = null!;
    
    [Export] private CharacterResource _operatorTemplate = null!;
    
    private static int _operatorCount = 0;

    public void HandleClick(GodotObject obj, Vector3 position, bool isDown)
    {
        if (!isDown)
            return;
        HandleClick(obj);
    }

    public void HandleClick(GodotObject obj)
    {
        if (obj is SiteNode siteNode &&
            siteNode.IsActive &&
            DispatchNavSpawnQueries.TryGetSpawnPoint(
                _dispatchNav.Graph,
                _spawnWorldPosition.GlobalPosition,
                out var spawnAnchor))
        {
            var site = siteNode.Site;
            GD.Print($"Clicked on site: {site.Label}");
            DispatchToSite(spawnAnchor.Position, site);
        }

        if (obj is CharacterResource characterResource)
        {
            DispatchToTail(characterResource);
        }
    }

    private void DispatchToSite(Vector3 spawnPosition, Site site)
    {
        var endPoint = DispatchNavQueries.GetClosestPointOnGraph(_dispatchNav.Graph, site.GlobalPosition);
        var path = DispatchNavPathfinder.FindPath(_dispatchNav.Graph, spawnPosition, endPoint);

        var character = _operatorTemplate.ToCharacter();
        character.IsOperator = true;
        character.Position.Set(path.StartPosition);

        _simulationController.World.RegisterCharacter(character);

        // Determine if the chosen option is a stakeout. Stakeout movements stop at
        // the entry position without entering the site, so destination must be null.
        var option = site.AvailableOptions.Length > 0 ? site.AvailableOptions[0] : null;
        var isStakeout = option?.VisionType == OperationVisionType.Stakeout;

        var movement = new Movement(
            id: Guid.NewGuid().ToString(),
            character: character,
            origin: null,
            destination: isStakeout ? null : site,
            path: path);

        var operation = option != null
            ? option.ToOperation(movement, site)
            : new Operation(
                id: Guid.NewGuid().ToString(),
                label: $"Visit {site.Label}",
                duration: 10.0)
            {
                SiteContext = site,
                MovementContext = movement
            };

        var kind = isStakeout ? AssignmentKind.StakeoutSite : AssignmentKind.VisitSite;

        var assignment = new Assignment(
            id: Guid.NewGuid().ToString(),
            character: character,
            operation: operation,
            currentMovement: movement,
            kind: kind)
        {
            CompletionBehavior = AssignmentCompletionBehavior.ReturnToBase,
            BaseWorldPosition = _operatorBaseWorldPosition.GlobalPosition,
            Source = AssignmentSource.PlayerOrder
        };

        _simulationController.EventBus.Publish(
            new AssignmentCreatedEvent(assignment, _simulationController.World.Time));
    }
    
    private void DispatchToTail(CharacterResource characterResource)
    {
        // Resolve the runtime target character.
        Character targetCharacter;
        try
        {
            targetCharacter = _simulationController.World.GetCharacter(characterResource.CharacterId);
        }
        catch
        {
            GD.PushWarning($"[CityscapeClickHandler] Cannot tail '{characterResource.CharacterId}': not found in WorldState.");
            return;
        }

        // Spawn the operator at the configured spawn point.
        if (!DispatchNavSpawnQueries.TryGetSpawnPoint(
                _dispatchNav.Graph,
                _spawnWorldPosition.GlobalPosition,
                out var spawnAnchor))
        {
            GD.PushWarning("[CityscapeClickHandler] No spawn point found for tail operator.");
            return;
        }

        var operatorCharacter = _operatorTemplate.ToCharacter();
        operatorCharacter.IsOperator = true;
        operatorCharacter.Position.Set(spawnAnchor.Position);

        _simulationController.World.RegisterCharacter(operatorCharacter);

        var assignment = new Assignment(
            id: Guid.NewGuid().ToString(),
            character: operatorCharacter,
            targetCharacter: targetCharacter)
        {
            CompletionBehavior = AssignmentCompletionBehavior.ReturnToBase,
            BaseWorldPosition = _operatorBaseWorldPosition.GlobalPosition,
            Source = AssignmentSource.PlayerOrder
        };

        _simulationController.EventBus.Publish(
            new AssignmentCreatedEvent(assignment, _simulationController.World.Time));
    }
}