using Godot;
using SurveillanceStategodot.scripts.navigation.authoring;
using SurveillanceStategodot.scripts.navigation.query;
using SurveillanceStategodot.scripts.navigation.runtime;

namespace SurveillanceStategodot.scripts.interaction;

public partial class CityscapeClickHandler : Node
{
    
    [Export] private PackedScene _agentScene;
    [Export] private DispatchNav _dispatchNav;
    [Export] private DispatchNavAgentSpawner _spawner;
    [Export] private Node3D _spawnWorldPosition;
    
    public void HandleClick(GodotObject obj, Vector3 position, bool isDown)
    {
        if(!isDown) return;
        if (DispatchNavSpawnQueries.TryGetSpawnPoint(_dispatchNav.Graph, _spawnWorldPosition.GlobalPosition, out var spawnAnchor))
        {
            var follower = _spawner.SpawnOnGraph(_agentScene, _spawnWorldPosition.GlobalPosition);
            if (follower != null)
            {
                var endPoint = DispatchNavQueries.GetClosestPointOnGraph(_dispatchNav.Graph, position);
                var path = DispatchNavPathfinder.FindPath(_dispatchNav.Graph, spawnAnchor, endPoint);
                if (path.IsValid)
                    follower.SetPath(path, snapToStart: true);
            }
        }
    }
}