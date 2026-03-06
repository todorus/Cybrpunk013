using Godot;

namespace SurveillanceStategodot.scripts.control.camera;

public partial class PanCamera : Camera3D
{
    [Export]
    public float PanSpeed { get; set; } = 0.2f;

    private void HandlePan(GodotObject obj, Vector3 position, Vector2 delta)
    {
        var delta3 = new Vector3(delta.X, 0, delta.Y);
        Position += delta3 * PanSpeed;
    }
}