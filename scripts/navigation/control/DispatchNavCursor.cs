using Godot;
using SurveillanceStategodot.scripts.navigation.authoring;
using SurveillanceStategodot.scripts.navigation.query;

namespace SurveillanceStategodot.scripts.navigation.control;

public partial class DispatchNavCursor : Node3D
{
    [Export] public DispatchNav DispatchNav;

    public void OnMove(GodotObject obj, Vector3 position, Vector2 delta)
    {
        if (DispatchNav?.Graph == null)
            return;

        GraphHit hit = DispatchNavQueries.GetClosestPointOnGraph(DispatchNav.Graph, position);

        if (hit.Valid)
            GlobalPosition = hit.Position;
    }
}