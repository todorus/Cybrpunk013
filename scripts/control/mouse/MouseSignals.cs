using Godot;

namespace SurveillanceStategodot.scripts.control.mouse;

public partial class MouseSignals : Node
{
    [Export]
    public Camera3D Camera { get; set; }
    
    [Export(PropertyHint.Layers3DPhysics)]
    public uint ObjectCollisionMask { get; set; } = 1;
    
    [Export(PropertyHint.Layers3DPhysics)]
    public uint PositionCollisionMask { get; set; } = 1;
    
    [Signal]
    public delegate void LeftMouseEventHandler(GodotObject obj, Vector3 position, bool isDown);
    [Signal]
    public delegate void RightMouseEventHandler(GodotObject obj, Vector3 position, bool isDown);
    [Signal]
    public delegate void MiddleMouseEventHandler(GodotObject obj, Vector3 position, bool isDown);
    
    [Signal]
    public delegate void MoveEventHandler(GodotObject obj, Vector3 position, Vector2 delta);
    
    [Signal]
    public delegate void PanEventHandler(GodotObject obj, Vector3 position, Vector2 delta);

    public override void _UnhandledInput(InputEvent @event)
    {
        base._Input(@event);
        var obj = GetObjectUnderMouse();
        var position = GetMousePositionInWorld();
        
        if (@event is InputEventMouseMotion mouseMotion)
        {
            EmitSignalMove(obj, position, mouseMotion.Relative);
        } else if (@event is InputEventMouseButton mouseButton)
        {
            HandleButton(mouseButton.ButtonIndex, mouseButton.Pressed, obj, position);
        } else if (@event is InputEventPanGesture panGesture) 
        {
            EmitSignalPan(obj, position, panGesture.Delta);
        }
    }
    
    private void HandleButton(MouseButton buttonIndex, bool pressed, GodotObject obj, Vector3 position)
    {
        switch (buttonIndex)
        {
            case MouseButton.Left:
                EmitSignalLeftMouse(obj, position, pressed);
                break;
            case MouseButton.Right:
                EmitSignalRightMouse(obj, position, pressed);
                break;
            case MouseButton.Middle:
                EmitSignalMiddleMouse(obj, position, pressed);
                break;
        }
    }

    private GodotObject GetObjectUnderMouse()
    {
        var result = this.ShootRayFromCamera(Camera, ObjectCollisionMask);
        if (result.Count == 0)
        {
            return null;
        }
        
        var collider = result["collider"];
        var obj = collider.Obj;
        
        if (obj is HitBox hitBox)
        {
            return hitBox.Target;
        }
        if (obj is GodotObject godotObject)
        {
            return godotObject;
        }

        return null;
    }
    
    private Vector3 GetMousePositionInWorld()
    {
        var result = this.ShootRayFromCamera(Camera, PositionCollisionMask);
        if (result.Count == 0)
        {
            return Vector3.Zero;
        }
        
        return (Vector3)result["position"];
    }
}