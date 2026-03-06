using Godot;

namespace SurveillanceStategodot.scripts.control.mouse;

public partial class HitBox : Node3D
{
    [Export]
    public Node3D Target { get; set; }
}