using Godot;

namespace SurveillanceStategodot.scripts.navigation.query;

public struct GraphHit
{
    public bool Valid;
    public int FromNode;
    public int ToNode;
    public Vector3 Position;
    public float DistanceSquared;
}