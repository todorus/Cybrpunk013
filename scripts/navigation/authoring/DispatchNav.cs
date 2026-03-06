using System.Collections.Generic;
using Godot;
using SurveillanceStategodot.scripts.navigation.graph;

namespace SurveillanceStategodot.scripts.navigation.authoring;

[Tool]
public partial class DispatchNav : Node3D
{
    [Export]
    public bool RebuildGraphButton
    {
        get => false;
        set
        {
            RebuildGraph();
        }
    }

    [Export]
    public DispatchNavGraph Graph { get; private set; }
    
    [ExportGroup("Preview")]

    [Export]
    public Color NodeColor { get; set; } = new Color(1f, 0.8f, 0.2f);

    [Export]
    public Color EdgeColor { get; set; } = new Color(0.2f, 0.9f, 1f);

    [Export]
    public float NodeRadius { get; set; } = 0.25f;

    [Export]
    public float EdgeThickness { get; set; } = 2f;

    public void RebuildGraph()
    {
        var nodes = CollectNavNodes();
        Graph = DispatchNavGraphBuilder.RebuildGraph(nodes);

        GD.Print("Dispatch graph rebuilt");
        NotifyPropertyListChanged();
        UpdateGizmos();
    }

    private List<NavNode> CollectNavNodes()
    {
        var list = new List<NavNode>();
        Collect(this, list);
        return list;
    }

    private static void Collect(Node node, List<NavNode> result)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is NavNode nav)
                result.Add(nav);

            Collect(child, result);
        }
    }
}