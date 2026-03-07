using Godot;
using SurveillanceStategodot.scripts.navigation.graph;

namespace SurveillanceStategodot.scripts.navigation.query;

public static class DispatchNavGraphExt
{
    public static bool HasDirectedEdge(this DispatchNavGraph graph, int from, int to)
    {
        if (graph == null || from < 0 || from >= graph.Nodes.Count)
            return false;

        var outgoing = graph.Nodes[from].Outgoing;
        for (int i = 0; i < outgoing.Count; i++)
        {
            if (outgoing[i] == to)
                return true;
        }

        return false;
    }

    public static float GetEdgeLength(this DispatchNavGraph graph, int from, int to)
    {
        return graph.Nodes[from].Position.DistanceTo(graph.Nodes[to].Position);
    }

    public static float GetDistanceAlongEdge(
        DispatchNavGraph graph,
        int from,
        int to,
        float t0,
        float t1)
    {
        return Mathf.Abs(t1 - t0) * GetEdgeLength(graph, from, to);
    }

    public static float GetAnchorToNodeDistance(
        this DispatchNavGraph graph,
        DispatchNavEdgeAnchor anchor,
        int nodeIndex)
    {
        float edgeLength = GetEdgeLength(graph, anchor.FromNode, anchor.ToNode);

        if (nodeIndex == anchor.FromNode)
            return anchor.T * edgeLength;

        if (nodeIndex == anchor.ToNode)
            return (1f - anchor.T) * edgeLength;

        return float.PositiveInfinity;
    }

    public static float GetNodeToAnchorDistance(
        this DispatchNavGraph graph,
        int nodeIndex,
        DispatchNavEdgeAnchor anchor)
    {
        return GetAnchorToNodeDistance(graph, anchor, nodeIndex);
    }

    public static float ComputeEdgeT(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector3 ab = b - a;
        float lenSq = ab.LengthSquared();
        if (lenSq <= Mathf.Epsilon)
            return 0f;

        float t = ab.Dot(p - a) / lenSq;
        return Mathf.Clamp(t, 0f, 1f);
    }
}