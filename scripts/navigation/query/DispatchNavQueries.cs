using SurveillanceStategodot.scripts.navigation.graph;
using Godot;

namespace SurveillanceStategodot.scripts.navigation.query;

public static class DispatchNavQueries
{
    public static DispatchNavEdgeAnchor GetClosestPointOnGraph(DispatchNavGraph graph, Vector3 worldPos)
    {
        DispatchNavEdgeAnchor best = new DispatchNavEdgeAnchor
        {
            Valid = false
        };
        float bestDistanceSquared = float.MaxValue;

        for (int i = 0; i < graph.Nodes.Count; i++)
        {
            var from = graph.Nodes[i];

            foreach (int toIndex in from.Outgoing)
            {
                var to = graph.Nodes[toIndex];

                Vector3 p = Geometry3D.GetClosestPointToSegment(worldPos, from.Position, to.Position);
                float t = DispatchNavGraphExt.ComputeEdgeT(from.Position, to.Position, p);
                float d2 = worldPos.DistanceSquaredTo(p);

                if (d2 < bestDistanceSquared)
                {
                    best.Valid = true;
                    best.FromNode = i;
                    best.ToNode = toIndex;
                    best.T = t;
                    best.Position = p;
                    bestDistanceSquared = d2;
                }
            }
        }

        return best;
    }
}