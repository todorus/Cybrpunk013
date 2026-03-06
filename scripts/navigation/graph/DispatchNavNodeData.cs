using Godot;
using Godot.Collections;

namespace SurveillanceStategodot.scripts.navigation.graph;

[GlobalClass]
public partial class DispatchNavNodeData : Resource
{
    [Export] 
    public string Id { get; set; } = "";
    [Export] 
    public Vector3 Position { get; set; } = Vector3.Zero;
    [Export] 
    public Array<int> Outgoing { get; set; } = new();
}