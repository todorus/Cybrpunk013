using Godot;
using Godot.Collections;

namespace SurveillanceStategodot.scripts.navigation.graph;

[GlobalClass]
public partial class DispatchNavGraph : Resource
{
    [Export] public Array<DispatchNavNodeData> Nodes { get; set; } = new();
}