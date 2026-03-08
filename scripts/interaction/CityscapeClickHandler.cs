using System;
using Godot;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.navigation.authoring;
using SurveillanceStategodot.scripts.navigation.query;

namespace SurveillanceStategodot.scripts.interaction;

public partial class CityscapeClickHandler : Node
{
    [Export] private DispatchNav _dispatchNav;
    [Export] private Node3D _spawnWorldPosition;
    [Export] private SimulationController _simulationController;
    
    public void HandleClick(GodotObject obj, Vector3 position, bool isDown)
    {
        if(!isDown) return;
        if (DispatchNavSpawnQueries.TryGetSpawnPoint(_dispatchNav.Graph, _spawnWorldPosition.GlobalPosition, out var spawnAnchor))
        {
            var endPoint = DispatchNavQueries.GetClosestPointOnGraph(_dispatchNav.Graph, position);
            var path = DispatchNavPathfinder.FindPath(_dispatchNav.Graph, spawnAnchor, endPoint);
            
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
}