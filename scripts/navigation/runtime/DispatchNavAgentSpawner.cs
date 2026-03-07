using Godot;
using SurveillanceStategodot.scripts.navigation.authoring;
using SurveillanceStategodot.scripts.navigation.query;

namespace SurveillanceStategodot.scripts.navigation.runtime;

public partial class DispatchNavAgentSpawner : Node
{
    [Export]
    public DispatchNav DispatchNav { get; set; }

    [Export]
    public Node SpawnParent { get; set; }

    public DispatchNavPathFollower SpawnOnGraph(PackedScene scene, Vector3 desiredWorldPosition)
    {
        if (scene == null)
        {
            GD.PushError($"{nameof(DispatchNavAgentSpawner)}: SpawnOnGraph failed because scene is null.");
            return null;
        }

        if (DispatchNav == null)
        {
            GD.PushError($"{nameof(DispatchNavAgentSpawner)}: SpawnOnGraph failed because DispatchNav is not assigned.");
            return null;
        }

        if (DispatchNav.Graph == null)
        {
            GD.PushError($"{nameof(DispatchNavAgentSpawner)}: SpawnOnGraph failed because DispatchNav.Graph is null.");
            return null;
        }

        if (!DispatchNavSpawnQueries.TryGetSpawnPoint(DispatchNav.Graph, desiredWorldPosition, out var anchor))
        {
            GD.PushError($"{nameof(DispatchNavAgentSpawner)}: SpawnOnGraph failed because no valid graph point was found.");
            return null;
        }

        Node instance = scene.Instantiate();
        if (instance is not DispatchNavPathFollower follower)
        {
            instance.QueueFree();
            GD.PushError($"{nameof(DispatchNavAgentSpawner)}: Spawned scene root must inherit {nameof(DispatchNavPathFollower)}.");
            return null;
        }

        Node parent = SpawnParent ?? GetTree().CurrentScene ?? this;
        parent.AddChild(follower);

        // Keep authored transforms stable if scene has local offsets.
        follower.GlobalPosition = anchor.Position;

        return follower;
    }
    
    public DispatchNavPathFollower SpawnOnGraphAndDispatch(
        PackedScene scene,
        Vector3 desiredSpawnWorldPosition,
        Vector3 desiredTargetWorldPosition,
        bool snapToPathStart = true)
    {
        if (scene == null || DispatchNav == null || DispatchNav.Graph == null)
            return null;

        if (!DispatchNavSpawnQueries.TryGetSpawnPoint(DispatchNav.Graph, desiredSpawnWorldPosition, out var spawnAnchor))
            return null;

        DispatchNavEdgeAnchor targetHit = DispatchNavQueries.GetClosestPointOnGraph(DispatchNav.Graph, desiredTargetWorldPosition);
        if (!targetHit.Valid)
            return null;

        var follower = SpawnOnGraph(scene, desiredSpawnWorldPosition);
        if (follower == null)
            return null;

        var path = DispatchNavPathfinder.FindPath(
            DispatchNav.Graph,
            spawnAnchor,
            targetHit
        );

        if (!path.IsValid)
        {
            follower.QueueFree();
            GD.PushWarning($"{nameof(DispatchNavAgentSpawner)}: No valid path from spawn to target.");
            return null;
        }

        follower.SetPath(path, snapToPathStart);
        return follower;
    }
}