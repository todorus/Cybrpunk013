using Godot;

namespace SurveillanceStategodot.scripts.presentation.primitives;

public partial class ResourceClickable : Control
{

    [Signal]
    public delegate void ClickedEventHandler(Resource resource);
    
    [Export]
    public Resource Resource;
    public void SetResource(Resource resource)
    {
        Resource = resource;
    }
    
    public ResourceClickable()
    {
        MouseDefaultCursorShape = CursorShape.PointingHand;
    }
    
    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            EmitSignalClicked(Resource);
        }
    }
    
}