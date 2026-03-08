

using Godot;

namespace SurveillanceStategodot.scripts.navigation.query;

public static class DispatchNavPathExt
{
    public readonly record struct AdvanceResult(
        Vector3 Position,
        Vector3 Direction,
        int SegmentIndex,
        bool ReachedDestination
    );

    public static AdvanceResult Advance(
        this DispatchNavPath path,
        int segmentIndex,
        Vector3 currentPosition,
        float travelDistance)
    {
        Vector3 newPosition = currentPosition;
        int idx = segmentIndex;

        while (idx < path.WorldPoints.Count - 1)
        {
            Vector3 a = newPosition;
            Vector3 b = path.WorldPoints[idx + 1];
            float dist = a.DistanceTo(b);

            if (dist <= 0.001f)
            {
                idx++;
                continue;
            }

            if (travelDistance < dist)
            {
                newPosition = a.MoveToward(b, travelDistance);
                return new AdvanceResult(newPosition, newPosition - currentPosition, idx, false);
            }

            newPosition = b;
            travelDistance -= dist;
            idx++;
        }

        bool reached = idx >= path.WorldPoints.Count - 1 &&
                       newPosition.DistanceTo(path.EndPosition) <= 0.05f;
        return new AdvanceResult(newPosition, newPosition - currentPosition, idx, reached);
    }
}