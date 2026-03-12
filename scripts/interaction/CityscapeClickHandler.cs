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
    }

    private void DispatchToSite(Vector3 spawnPosition, Site site)
    {
        var endPoint = DispatchNavQueries.GetClosestPointOnGraph(_dispatchNav.Graph, site.GlobalPosition);
        var path = DispatchNavPathfinder.FindPath(_dispatchNav.Graph, spawnPosition, endPoint);

        var character = _operatorTemplate.ToCharacter();
        character.IsOperator = true;
        
        var movement = new Movement(
            id: Guid.NewGuid().ToString(),
            character: character,
            origin: null,
            destination: site,
            path: path,
            initialPosition: path.StartPosition);

        var operation = EnsureOperation(site, movement);

        var assignment = new Assignment(
            id: Guid.NewGuid().ToString(),
            character: character,
            operation: operation,
            currentMovement: movement)
        {
            CompletionBehavior = AssignmentCompletionBehavior.ReturnToBase,
            BaseWorldPosition = _operatorBaseWorldPosition.GlobalPosition,
            Source = AssignmentSource.PlayerOrder
        };

        _simulationController.EventBus.Publish(
            new AssignmentCreatedEvent(assignment, _simulationController.World.Time));
    }

    private static Operation EnsureOperation(Site site, Movement movement)
    {
        if (site.AvailableOptions.Length == 0)
        {
            var operation = new Operation(
                id: Guid.NewGuid().ToString(),
                label: $"Visit {site.Label}",
                duration: 10.0)
            {
                SiteContext = site,
                MovementContext = movement
            };
            return operation;
        }
        
        var option = site.AvailableOptions[0];
        return option.ToOperation(movement, site);
    }
}