using Godot;
using Godot.Collections;

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
}