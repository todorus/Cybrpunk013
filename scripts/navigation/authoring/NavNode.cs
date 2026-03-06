using Godot;
using Godot.Collections;

namespace SurveillanceStategodot.scripts.navigation.authoring;

[Tool]
public partial class NavNode : Node3D
{
    public string Id => Name;

    // Outgoing edges only.
    [Export] public Array<NavNode> ConnectedNodes { get; set; } = new();
}