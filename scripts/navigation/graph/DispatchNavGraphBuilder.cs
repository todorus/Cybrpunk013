using System.Collections.Generic;
using Godot.Collections;

namespace SurveillanceStategodot.scripts.navigation.graph;

public static class DispatchNavGraphBuilder
{
    public static DispatchNavGraph RebuildGraph(IReadOnlyList<authoring.NavNode> sourceNodes)
    {
        var graph = new DispatchNavGraph();
        var dataNodes = new Array<DispatchNavNodeData>();
        var indexByNode = new Godot.Collections.Dictionary<authoring.NavNode, int>();

        for (int i = 0; i < sourceNodes.Count; i++)
        {
            indexByNode[sourceNodes[i]] = i;
        }

        // First pass: create node data
        for (int i = 0; i < sourceNodes.Count; i++)
        {
            var source = sourceNodes[i];

            dataNodes.Add(new DispatchNavNodeData
            {
                Id = string.IsNullOrWhiteSpace(source.Id) ? source.Name : source.Id,
                Position = source.GlobalPosition,
                Outgoing = new Array<int>()
            });
        }

        // Second pass: normalize outgoing edges
        for (int i = 0; i < sourceNodes.Count; i++)
        {
            var source = sourceNodes[i];
            var outgoingSet = new SortedSet<int>();

            foreach (var target in source.ConnectedNodes)
            {
                if (target == null)
                    continue;

                if (ReferenceEquals(source, target))
                    continue;

                if (!indexByNode.TryGetValue(target, out int targetIndex))
                    continue;

                outgoingSet.Add(targetIndex);
            }

            foreach (int targetIndex in outgoingSet)
            {
                dataNodes[i].Outgoing.Add(targetIndex);
            }
        }

        graph.Nodes = dataNodes;
        return graph;
    }
}