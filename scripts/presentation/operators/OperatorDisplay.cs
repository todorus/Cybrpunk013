using Godot;
using SurveillanceStategodot.scripts.authoring;
using SurveillanceStategodot.scripts.presentation.portrait;

namespace SurveillanceStategodot.scripts.presentation.operators;

public partial class OperatorDisplay : Control
{
    [Signal]
    public delegate void AvatarChangedEventHandler(Texture2D newAvatar);

    [Export] 
    private PortraitCache _portraitCache;

    [Export] 
    private CharacterResource _operatorResource;
    
    public override void _Ready()
    {
        // Defer so that PortraitCache (and its PortraitStudio) have fully
        // initialised their _Ready before we attempt the first render.
        CallDeferred(MethodName.RefreshAvatar);
    }

    private async void RefreshAvatar()
    {
        var avatar = await _portraitCache.GetOrRenderAsync(_operatorResource);
        EmitSignalAvatarChanged(avatar);
    }
}