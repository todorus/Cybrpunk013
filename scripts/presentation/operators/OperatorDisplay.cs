using Godot;

namespace SurveillanceStategodot.scripts.presentation.operators;

public partial class OperatorDisplay : Control
{
    [Signal]
    public delegate void AvatarChangedEventHandler(Texture2D newAvatar);
    
    // public override void _Ready()
    // {
    //     // Defer so that PortraitCache (and its PortraitStudio) have fully
    //     // initialised their _Ready before we attempt the first render.
    //     CallDeferred(MethodName.RefreshAvatar);
    // }

    public Texture2D Avatar
    {
        set => EmitSignalAvatarChanged(value);
    }
}