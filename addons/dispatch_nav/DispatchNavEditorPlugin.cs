using Godot;

namespace SurveillanceStategodot.addons.dispatch_nav;

#if TOOLS


[Tool]
public partial class DispatchNavEditorPlugin : EditorPlugin
{
    private DispatchNavGizmoPlugin _gizmoPlugin;

    public override void _EnterTree()
    {
        GD.Print("DispatchNavEditorPlugin entering tree");
        _gizmoPlugin = new DispatchNavGizmoPlugin();
        AddNode3DGizmoPlugin(_gizmoPlugin);
    }

    public override void _ExitTree()
    {
        if (_gizmoPlugin != null)
            RemoveNode3DGizmoPlugin(_gizmoPlugin);
    }
}
#endif