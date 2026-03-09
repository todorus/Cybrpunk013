using System;
using Godot;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.navigation.authoring;
using SurveillanceStategodot.scripts.navigation.query;
using SurveillanceStategodot.scripts.presentation.sites;

namespace SurveillanceStategodot.scripts.interaction;

public partial class CityscapeClickHandler : Node
{
    [Export] private DispatchNav _dispatchNav;
    [Export] private Node3D _spawnWorldPosition;
    [Export] private SimulationController _simulationController;
    
    public void HandleClick(GodotObject obj, Vector3 position, bool isDown)
    {
        if(!isDown) return;
        
        if(obj is SiteNode siteNode && siteNode.IsActive && DispatchNavSpawnQueries.TryGetSpawnPoint(_dispatchNav.Graph, _spawnWorldPosition.GlobalPosition, out var spawnAnchor))
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
            
        var movement = new Movement(
            id: Guid.NewGuid().ToString(),
            character: null,
            origin: null,
            destination: null,
            path: path,
            initialPosition: path.StartPosition);

        _simulationController.EventBus.Publish(
            new MovementStartedEvent(movement, _simulationController.World.Time));
    }
}