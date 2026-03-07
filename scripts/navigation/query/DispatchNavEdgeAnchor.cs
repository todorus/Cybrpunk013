using Godot;

namespace SurveillanceStategodot.scripts.navigation.query;

/// <summary>
/// A precise point located on a directed graph edge.
/// T is the normalized position from FromNode -> ToNode.
/// </summary>
public struct DispatchNavEdgeAnchor
{
    public bool Valid;
    public int FromNode;
    public int ToNode;
    public float T;
    public Vector3 Position;

    public static DispatchNavEdgeAnchor Invalid => new()
    {
        Valid = false,
        FromNode = -1,
        ToNode = -1,
        T = 0f,
        Position = Vector3.Zero
    };
}