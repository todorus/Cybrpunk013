using System.Collections.Generic;
using Godot;
using SurveillanceStategodot.scripts.navigation.authoring;

namespace SurveillanceStategodot.addons.dispatch_nav;

#if TOOLS
[Tool]
public partial class DispatchNavGizmoPlugin : EditorNode3DGizmoPlugin
{
    public DispatchNavGizmoPlugin()
    {
        AddMaterial("dispatch_nav_edges", MakeMaterial(Colors.Cyan));
        AddMaterial("dispatch_nav_nodes", MakeMaterial(Colors.Yellow));
    }

    public override string _GetGizmoName()
    {
        return "DispatchNavGizmo";
    }

    public override bool _HasGizmo(Node3D forNode3D)
    {
        bool result = forNode3D is DispatchNav;
        if (result)
            GD.Print($"Has gizmo for: {forNode3D.Name}");
        return result;
    }

    public override void _Redraw(EditorNode3DGizmo gizmo)
    {
        gizmo.Clear();

        if (gizmo.GetNode3D() is not DispatchNav dispatchNav)
            return;

        // Rebuild materials from current node colors.
        AddMaterial("dispatch_nav_edges", MakeMaterial(dispatchNav.EdgeColor));
        AddMaterial("dispatch_nav_nodes", MakeMaterial(dispatchNav.NodeColor));

        var edgeMaterial = GetMaterial("dispatch_nav_edges", gizmo);
        var nodeMaterial = GetMaterial("dispatch_nav_nodes", gizmo);

        var navNodes = new List<NavNode>();
        CollectNavNodes(dispatchNav, navNodes);

        var lines = new List<Vector3>();

        foreach (var node in navNodes)
        {
            var from = dispatchNav.ToLocal(node.GlobalPosition);

            foreach (var connected in node.ConnectedNodes)
            {
                if (connected == null)
                    continue;

                var to = dispatchNav.ToLocal(connected.GlobalPosition);
                lines.Add(from);
                lines.Add(to);
            }
        }

        if (lines.Count > 0)
            gizmo.AddLines(lines.ToArray(), edgeMaterial, false);

        var sphere = new SphereMesh
        {
            Radius = dispatchNav.NodeRadius,
            Height = dispatchNav.NodeRadius * 2
        };

        foreach (var node in navNodes)
        {
            var xform = new Transform3D(Basis.Identity, dispatchNav.ToLocal(node.GlobalPosition));
            gizmo.AddMesh(sphere, nodeMaterial, xform);
        }
    }

    private static StandardMaterial3D MakeMaterial(Color color)
    {
        var material = new StandardMaterial3D();
        material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        material.AlbedoColor = color;
        material.NoDepthTest = false;
        return material;
    }

    private static void CollectNavNodes(Node node, List<NavNode> result)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is NavNode navNode)
                result.Add(navNode);

            CollectNavNodes(child, result);
        }
    }
}
#endif