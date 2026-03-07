using System.Collections.Generic;
using Godot;
using Godot.Collections;
using SurveillanceStategodot.scripts.navigation.graph;

namespace SurveillanceStategodot.scripts.navigation.query;

public static class DispatchNavPathfinder
{
    private struct AnchorCandidate
    {
        public int NodeIndex;
        public float Cost;
    }

    public static DispatchNavPath FindPath(
        DispatchNavGraph graph,
        Vector3 startWorldPos,
        Vector3 endWorldPos)
    {
        if (graph == null || graph.Nodes.Count == 0)
            return InvalidPath();

        DispatchNavEdgeAnchor startHit = DispatchNavQueries.GetClosestPointOnGraph(graph, startWorldPos);
        DispatchNavEdgeAnchor endHit = DispatchNavQueries.GetClosestPointOnGraph(graph, endWorldPos);

        if (!startHit.Valid || !endHit.Valid)
            return InvalidPath();

        return FindPath(graph, startHit, endHit);
    }

    public static DispatchNavPath FindPath(
        DispatchNavGraph graph,
        DispatchNavEdgeAnchor start,
        Vector3 endWorldPos)
    {
        if (graph == null || graph.Nodes.Count == 0)
            return InvalidPath();

        DispatchNavEdgeAnchor endHit = DispatchNavQueries.GetClosestPointOnGraph(graph, endWorldPos);
        if (!endHit.Valid)
            return InvalidPath();

        return FindPath(graph, start, endHit);
    }

    public static DispatchNavPath FindPath(
        DispatchNavGraph graph,
        Vector3 startWorldPos,
        DispatchNavEdgeAnchor end)
    {
        if (graph == null || graph.Nodes.Count == 0)
            return InvalidPath();

        DispatchNavEdgeAnchor startHit = DispatchNavQueries.GetClosestPointOnGraph(graph, startWorldPos);
        if (!startHit.Valid)
            return InvalidPath();

        return FindPath(graph, startHit, end);
    }

    public static DispatchNavPath FindPath(
        DispatchNavGraph graph,
        DispatchNavEdgeAnchor start,
        DispatchNavEdgeAnchor end)
    {
        if (graph == null || !start.Valid || !end.Valid)
            return InvalidPath();

        // Same physical edge, same direction
        if (start.FromNode == end.FromNode && start.ToNode == end.ToNode)
        {
            return BuildSameEdgePath(start, end);
        }

        // Same physical edge, reversed direction
        if (start.FromNode == end.ToNode && start.ToNode == end.FromNode)
        {
            return BuildSameEdgePath(start, end);
        }

        var startCandidates = GetStartCandidates(graph, start);
        var endCandidates = GetEndCandidates(graph, end);

        float bestCost = float.MaxValue;
        List<int> bestNodePath = null;

        foreach (var startCandidate in startCandidates)
        {
            foreach (var endCandidate in endCandidates)
            {
                if (!DispatchNavAStar.TryFindPath(graph, startCandidate.NodeIndex, endCandidate.NodeIndex, out var nodePath))
                    continue;

                float nodePathCost = ComputeNodePathLength(graph, nodePath);
                float totalCost = startCandidate.Cost + nodePathCost + endCandidate.Cost;

                if (totalCost < bestCost)
                {
                    bestCost = totalCost;
                    bestNodePath = nodePath;
                }
            }
        }

        if (bestNodePath == null)
            return InvalidPath();

        return BuildFullPath(graph, start, end, bestNodePath);
    }

    private static List<AnchorCandidate> GetStartCandidates(DispatchNavGraph graph, DispatchNavEdgeAnchor start)
    {
        var result = new List<AnchorCandidate>(2)
        {
            new AnchorCandidate
            {
                NodeIndex = start.FromNode,
                Cost = graph.GetAnchorToNodeDistance(start, start.FromNode)
            },
            new AnchorCandidate
            {
                NodeIndex = start.ToNode,
                Cost = graph.GetAnchorToNodeDistance(start, start.ToNode)
            }
        };

        return result;
    }

    private static List<AnchorCandidate> GetEndCandidates(DispatchNavGraph graph, DispatchNavEdgeAnchor end)
    {
        var result = new List<AnchorCandidate>(2)
        {
            new AnchorCandidate
            {
                NodeIndex = end.FromNode,
                Cost = graph.GetNodeToAnchorDistance(end.FromNode, end)
            },
            new AnchorCandidate
            {
                NodeIndex = end.ToNode,
                Cost = graph.GetNodeToAnchorDistance(end.ToNode, end)
            }
        };

        return result;
    }

    private static DispatchNavPath BuildSameEdgePath(
        DispatchNavEdgeAnchor start,
        DispatchNavEdgeAnchor end)
    {
        var path = new DispatchNavPath
        {
            IsValid = true,
            StartPosition = start.Position,
            EndPosition = end.Position,
            StartFromNode = start.FromNode,
            StartToNode = start.ToNode,
            StartEdgeT = start.T,
            EndFromNode = end.FromNode,
            EndToNode = end.ToNode,
            EndEdgeT = end.T,
            NodeSequence = new Array<int>(),
            WorldPoints = new Array<Vector3> { start.Position, end.Position }
        };

        return path;
    }

    private static DispatchNavPath BuildFullPath(
        DispatchNavGraph graph,
        DispatchNavEdgeAnchor start,
        DispatchNavEdgeAnchor end,
        List<int> nodePath)
    {
        var points = new Array<Vector3>();
        points.Add(start.Position);

        for (int i = 0; i < nodePath.Count; i++)
        {
            points.Add(graph.Nodes[nodePath[i]].Position);
        }

        points.Add(end.Position);

        return new DispatchNavPath
        {
            IsValid = true,
            StartPosition = start.Position,
            EndPosition = end.Position,
            StartFromNode = start.FromNode,
            StartToNode = start.ToNode,
            StartEdgeT = start.T,
            EndFromNode = end.FromNode,
            EndToNode = end.ToNode,
            EndEdgeT = end.T,
            NodeSequence = new Array<int>(nodePath.ToArray()),
            WorldPoints = points
        };
    }

    private static float ComputeNodePathLength(DispatchNavGraph graph, List<int> path)
    {
        float total = 0f;

        for (int i = 1; i < path.Count; i++)
        {
            total += graph.Nodes[path[i - 1]].Position.DistanceTo(graph.Nodes[path[i]].Position);
        }

        return total;
    }

    private static DispatchNavPath InvalidPath()
    {
        return new DispatchNavPath
        {
            IsValid = false
        };
    }
}