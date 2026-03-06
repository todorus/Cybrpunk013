using Godot;

namespace SurveillanceStategodot.scripts.navigation.authoring;

[Tool]
[GlobalClass]
public partial class NavNode : Node3D
{
    [Export] public string Id { get; set; } = "";

    [Export]
    public Godot.Collections.Array<NavNode> ConnectedNodes { get; set; } = new();

    public override void _Notification(int what)
    {
        if (!Engine.IsEditorHint())
            return;

        if (what == NotificationTransformChanged)
        {
            var dispatchNav = FindDispatchNavParent();
            dispatchNav?.CallDeferred(nameof(DispatchNav.RebuildGraph));
        }
    }

    private DispatchNav FindDispatchNavParent()
    {
        Node current = GetParent();

        while (current != null)
        {
            if (current is DispatchNav dispatchNav)
                return dispatchNav;

            current = current.GetParent();
        }

        return null;
    }
}