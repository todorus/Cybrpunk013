using Godot;
using SurveillanceStategodot.scripts.navigation.graph;

namespace SurveillanceStategodot.scripts.navigation.query;

public static class DispatchNavSpawnQueries
{
    public static bool TryGetSpawnPoint(
        DispatchNavGraph graph,
        Vector3 desiredWorldPosition,
        out DispatchNavEdgeAnchor anchor)
    {
        anchor = DispatchNavEdgeAnchor.Invalid;

        if (graph == null || graph.Nodes == null || graph.Nodes.Count == 0)
            return false;

        DispatchNavEdgeAnchor hit = DispatchNavQueries.GetClosestPointOnGraph(graph, desiredWorldPosition);
        if (!hit.Valid)
            return false;

        anchor = hit;
        return true;
    }

    public static bool TryGetSpawnPosition(
        DispatchNavGraph graph,
        Vector3 desiredWorldPosition,
        out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.Zero;

        if (!TryGetSpawnPoint(graph, desiredWorldPosition, out var anchor))
            return false;

        spawnPosition = anchor.Position;
        return true;
    }
}