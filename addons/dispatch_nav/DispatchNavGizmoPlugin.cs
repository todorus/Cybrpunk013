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

        var lines = BuildBidirectionalLines(dispatchNav, navNodes);

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

    private static List<Vector3> BuildBidirectionalLines(DispatchNav dispatchNav, List<NavNode> navNodes)
    {
        var result = new List<Vector3>();
        var nodeIndex = new Dictionary<NavNode, int>(navNodes.Count);
        var uniqueEdges = new HashSet<EdgeKey>();

        for (int i = 0; i < navNodes.Count; i++)
            nodeIndex[navNodes[i]] = i;

        foreach (var node in navNodes)
        {
            if (!nodeIndex.TryGetValue(node, out int fromIndex))
                continue;

            Vector3 from = dispatchNav.ToLocal(node.GlobalPosition);

            foreach (var connected in node.ConnectedNodes)
            {
                if (connected == null)
                    continue;

                if (!nodeIndex.TryGetValue(connected, out int toIndex))
                    continue;

                if (fromIndex == toIndex)
                    continue;

                var key = new EdgeKey(fromIndex, toIndex);
                if (!uniqueEdges.Add(key))
                    continue;

                Vector3 to = dispatchNav.ToLocal(connected.GlobalPosition);
                result.Add(from);
                result.Add(to);
            }
        }

        return result;
    }

    private static StandardMaterial3D MakeMaterial(Color color)
    {
        var material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = color,
            NoDepthTest = false
        };

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

    private readonly struct EdgeKey
    {
        public readonly int A;
        public readonly int B;

        public EdgeKey(int x, int y)
        {
            if (x < y)
            {
                A = x;
                B = y;
            }
            else
            {
                A = y;
                B = x;
            }
        }
    }
}
#endif