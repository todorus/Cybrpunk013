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

        // Second pass: normalize adjacency as bidirectional
        var outgoingSets = new List<SortedSet<int>>(sourceNodes.Count);
        for (int i = 0; i < sourceNodes.Count; i++)
        {
            outgoingSets.Add(new SortedSet<int>());
        }

        for (int i = 0; i < sourceNodes.Count; i++)
        {
            var source = sourceNodes[i];

            foreach (var target in source.ConnectedNodes)
            {
                if (target == null)
                    continue;

                if (ReferenceEquals(source, target))
                    continue;

                if (!indexByNode.TryGetValue(target, out int targetIndex))
                    continue;

                // Bake both directions into the graph.
                outgoingSets[i].Add(targetIndex);
                outgoingSets[targetIndex].Add(i);
            }
        }

        // Final pass: copy normalized adjacency into resource arrays
        for (int i = 0; i < sourceNodes.Count; i++)
        {
            foreach (int targetIndex in outgoingSets[i])
            {
                dataNodes[i].Outgoing.Add(targetIndex);
            }
        }

        graph.Nodes = dataNodes;
        return graph;
    }
}