using System.Collections.Generic;
using SurveillanceStategodot.scripts.navigation.graph;

namespace SurveillanceStategodot.scripts.navigation.query;

public static class DispatchNavAStar
{
    private sealed class NodeRecord
    {
        public int Node;
        public float G;
        public float F;
    }

    public static bool TryFindPath(
        DispatchNavGraph graph,
        int startNode,
        int goalNode,
        out List<int> path)
    {
        path = null;

        if (graph == null)
            return false;
        if (startNode < 0 || startNode >= graph.Nodes.Count)
            return false;
        if (goalNode < 0 || goalNode >= graph.Nodes.Count)
            return false;

        if (startNode == goalNode)
        {
            path = new List<int> { startNode };
            return true;
        }

        var open = new PriorityQueue<int, float>();
        var cameFrom = new Dictionary<int, int>();
        var gScore = new Dictionary<int, float>();
        var closed = new HashSet<int>();

        gScore[startNode] = 0f;
        open.Enqueue(startNode, Heuristic(graph, startNode, goalNode));

        while (open.Count > 0)
        {
            int current = open.Dequeue();

            if (closed.Contains(current))
                continue;

            if (current == goalNode)
            {
                path = ReconstructPath(cameFrom, current);
                return true;
            }

            closed.Add(current);

            var currentNode = graph.Nodes[current];
            foreach (int neighbor in currentNode.Outgoing)
            {
                if (neighbor < 0 || neighbor >= graph.Nodes.Count)
                    continue;

                if (closed.Contains(neighbor))
                    continue;

                float tentativeG = gScore[current] +
                    currentNode.Position.DistanceTo(graph.Nodes[neighbor].Position);

                if (!gScore.TryGetValue(neighbor, out float existingG) || tentativeG < existingG)
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    float f = tentativeG + Heuristic(graph, neighbor, goalNode);
                    open.Enqueue(neighbor, f);
                }
            }
        }

        return false;
    }

    private static float Heuristic(DispatchNavGraph graph, int from, int to)
    {
        return graph.Nodes[from].Position.DistanceTo(graph.Nodes[to].Position);
    }

    private static List<int> ReconstructPath(Dictionary<int, int> cameFrom, int current)
    {
        var result = new List<int> { current };

        while (cameFrom.TryGetValue(current, out int parent))
        {
            current = parent;
            result.Add(current);
        }

        result.Reverse();
        return result;
    }
}