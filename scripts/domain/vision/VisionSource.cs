using Godot;
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.domain.vision;

public sealed class VisionSource
{
    public string Id { get; }
    public Character? Owner { get; }
    public VisionSourceType Type { get; }
    public float Range { get; set; }
    public Vector3 WorldPosition { get; set; }
    public Site? SiteContext { get; set; }

    public VisionSource(string id, Character? owner, VisionSourceType type, float range)
    {
        Id = id;
        Owner = owner;
        Type = type;
        Range = range;
    }
}