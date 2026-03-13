using Godot;
using Godot.Collections;
using SurveillanceStategodot.scripts.navigation.graph;

namespace SurveillanceStategodot.scripts.navigation.query;

/// <summary>
/// A traversable route through the nav graph.
/// Supports endpoints that lie on edges rather than exactly on nodes.
/// </summary>
[GlobalClass]
public partial class DispatchNavPath : Resource
{
    [Export] public bool IsValid { get; set; }

    [Export] public Vector3 StartPosition { get; set; }
    [Export] public Vector3 EndPosition { get; set; }

    [Export] public int StartFromNode { get; set; } = -1;
    [Export] public int StartToNode { get; set; } = -1;
    [Export] public float StartEdgeT { get; set; }

    [Export] public int EndFromNode { get; set; } = -1;
    [Export] public int EndToNode { get; set; } = -1;
    [Export] public float EndEdgeT { get; set; }

    /// <summary>
    /// Intermediate graph nodes the agent should visit in order.
    /// Does not include the start/end edge positions themselves.
    /// </summary>
    [Export] public Array<int> NodeSequence { get; set; } = new();

    /// <summary>
    /// Optional baked world-space corners for immediate movement use.
    /// </summary>
    [Export] public Array<Vector3> WorldPoints { get; set; } = new();

    public float GetTotalLength()
    {
        float total = 0f;
        for (int i = 1; i < WorldPoints.Count; i++)
            total += WorldPoints[i - 1].DistanceTo(WorldPoints[i]);
        return total;
    }

    /// <summary>
    /// Attempts to update the path's endpoint by sliding it along the same edge.
    /// Returns true if the new target position projects onto the same edge as the
    /// current end — the last WorldPoint, EndPosition, EndEdgeT are updated in place.
    /// Returns false if the target has moved to a different edge, meaning a full
    /// repath is needed.
    /// </summary>
    public bool TrySlideEndPosition(DispatchNavGraph graph, Vector3 newTargetWorldPos)
    {
        if (!IsValid || WorldPoints.Count < 2)
            return false;

        if (EndFromNode < 0 || EndToNode < 0 ||
            EndFromNode >= graph.Nodes.Count || EndToNode >= graph.Nodes.Count)
            return false;

        var edgeFrom = graph.Nodes[EndFromNode].Position;
        var edgeTo = graph.Nodes[EndToNode].Position;

        // Project the new target position onto the end edge.
        var closest = Geometry3D.GetClosestPointToSegment(newTargetWorldPos, edgeFrom, edgeTo);
        float distSq = newTargetWorldPos.DistanceSquaredTo(closest);

        // Check if the new position is also the closest point on the entire graph
        // for this edge — use a generous tolerance so minor perpendicular drift
        // doesn't force a full repath.
        var edgeLen = edgeFrom.DistanceTo(edgeTo);
        float tolerance = edgeLen * 0.5f + 0.5f; // half the edge length + a small fixed margin
        if (distSq > tolerance * tolerance)
            return false;

        // Still on the same edge — slide the endpoint.
        float newT = DispatchNavGraphExt.ComputeEdgeT(edgeFrom, edgeTo, closest);

        EndEdgeT = newT;
        EndPosition = closest;
        WorldPoints[WorldPoints.Count - 1] = closest;

        return true;
    }
}