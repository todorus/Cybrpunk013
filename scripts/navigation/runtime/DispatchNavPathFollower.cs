using Godot;
using SurveillanceStategodot.scripts.navigation.query;

namespace SurveillanceStategodot.scripts.navigation.runtime;

public partial class DispatchNavPathFollower : Node3D
{
    [Signal] 
    public delegate void DestinationReachedEventHandler();
    
    [Export] 
    private float _speed = 3f;
    
    public DispatchNavPath CurrentPath { get; private set; }
    public int SegmentIndex { get; private set; }
    public bool HasPath => CurrentPath != null && CurrentPath.IsValid && CurrentPath.WorldPoints.Count >= 2;
    
    public override void _PhysicsProcess(double delta)
    {
        if (!HasPath) return;
        
        Vector3 nextPos;
        Advance(GlobalPosition, _speed * (float)delta, out nextPos);
        GlobalPosition = nextPos;
        ResetIfFinished();
    }

    private void ResetIfFinished()
    {
        if (!IsFinished(GlobalPosition)) return;
        
        CurrentPath = null;
        SegmentIndex = 0;
        EmitSignalDestinationReached();
    }

    public void SetPath(DispatchNavPath path)
    {
        CurrentPath = path;
        SegmentIndex = 0;
    }

    public Vector3 GetCurrentTarget()
    {
        if (!HasPath)
            return Vector3.Zero;

        int targetIndex = Mathf.Min(SegmentIndex + 1, CurrentPath.WorldPoints.Count - 1);
        return CurrentPath.WorldPoints[targetIndex];
    }

    public bool Advance(Vector3 currentPosition, float moveDistance, out Vector3 newPosition)
    {
        newPosition = currentPosition;

        if (!HasPath) return false;

        while (SegmentIndex < CurrentPath.WorldPoints.Count - 1)
        {
            Vector3 a = newPosition;
            Vector3 b = CurrentPath.WorldPoints[SegmentIndex + 1];
            float dist = a.DistanceTo(b);

            if (dist <= 0.001f)
            {
                SegmentIndex++;
                continue;
            }

            if (moveDistance < dist)
            {
                newPosition = a.MoveToward(b, moveDistance);
                return true;
            }

            newPosition = b;
            moveDistance -= dist;
            SegmentIndex++;
        }

        return true;
    }

    public bool IsFinished(Vector3 currentPosition, float epsilon = 0.05f)
    {
        if (!HasPath) return true;

        return SegmentIndex >= CurrentPath.WorldPoints.Count - 1 &&
               currentPosition.DistanceTo(CurrentPath.EndPosition) <= epsilon;
    }
}