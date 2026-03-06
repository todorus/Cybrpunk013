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
    
    private bool _rebuildQueued = false;

    public void RebuildGraph()
    {
        if (!Engine.IsEditorHint())
            return;

        if (_rebuildQueued)
            return;

        _rebuildQueued = true;
        CallDeferred(nameof(EditorRebuildGraphDeferred));
    }

    private void EditorRebuildGraphDeferred()
    {
        _rebuildQueued = false;

        var nodes = new List<NavNode>();
        CollectNavNodes(this, nodes);

        Graph = DispatchNavGraphBuilder.RebuildGraph(nodes);

        // Redraw this node's gizmo.
        UpdateGizmos();
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