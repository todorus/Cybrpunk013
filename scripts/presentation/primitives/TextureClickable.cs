using Godot;

namespace SurveillanceStategodot.scripts.presentation.primitives;

public partial class TextureClickable : TextureRect
{
    [Signal]
    public delegate void ClickedEventHandler(Resource resource);

    [Export]
    public Resource Resource;
    public void SetResource(Resource resource)
    {
        Resource = resource;
    }
    
    public TextureClickable()
    {
        MouseDefaultCursorShape = CursorShape.PointingHand;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            EmitSignalClicked(Resource);
            AcceptEvent();
        }
    }
}