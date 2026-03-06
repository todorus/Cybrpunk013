using SurveillanceStategodot.scripts.navigation.graph;

namespace SurveillanceStategodot.scripts.navigation.query;

using Godot;

public static class DispatchNavQueries
{
    public static GraphHit GetClosestPointOnGraph(DispatchNavGraph graph, Vector3 worldPos)
    {
        GraphHit best = new GraphHit
        {
            Valid = false,
            DistanceSquared = float.MaxValue
        };

        for (int i = 0; i < graph.Nodes.Count; i++)
        {
            var from = graph.Nodes[i];

            foreach (int toIndex in from.Outgoing)
            {
                var to = graph.Nodes[toIndex];

                Vector3 p = Geometry3D.GetClosestPointToSegment(
                    worldPos,
                    from.Position,
                    to.Position
                );

                float d2 = worldPos.DistanceSquaredTo(p);

                if (d2 < best.DistanceSquared)
                {
                    best.Valid = true;
                    best.FromNode = i;
                    best.ToNode = toIndex;
                    best.Position = p;
                    best.DistanceSquared = d2;
                }
            }
        }

        return best;
    }
}