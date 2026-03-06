using Godot;

namespace SurveillanceStategodot.scripts.control.mouse;

public partial class Cursor : Node3D
{
    public void OnMove(GodotObject obj, Vector3 position, Vector2 delta)
    {
        Position = position;
    }
}