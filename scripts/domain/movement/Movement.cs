using System;
using Godot;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.navigation.query;

namespace SurveillanceStategodot.scripts.domain.movement;

public sealed class Movement
{
    public string Id { get; }
    public Character? Character { get; }
    public Site? Origin { get; }
    public Site? Destination { get; }

    public Vector3 CurrentWorldPosition { get; private set; }
    public Vector3 CurrentForward { get; private set; } = Vector3.Forward;
    public bool HasArrived { get; private set; }

    public DispatchNavPath Path { get; }
    public int SegmentIndex { get; private set; }

    public event Action<Movement>? PositionChanged;
    public event Action<Movement>? Arrived;

    public Movement(
        string id,
        Character? character,
        Site? origin,
        Site? destination,
        DispatchNavPath path,
        Vector3 initialPosition)
    {
        Id = id;
        Character = character;
        Origin = origin;
        Destination = destination;
        Path = path;
        CurrentWorldPosition = initialPosition;
    }

    public void Advance(float travelDistance)
    {
        if (HasArrived || !Path.IsValid || Path.WorldPoints.Count < 2)
            return;

        var result = Path.Advance(SegmentIndex, CurrentWorldPosition, travelDistance);

        var oldPosition = CurrentWorldPosition;

        CurrentWorldPosition = result.Position;
        SegmentIndex = result.SegmentIndex;

        if (result.Direction.LengthSquared() > 0.0001f)
            CurrentForward = result.Direction.Normalized();

        if (oldPosition != CurrentWorldPosition)
            PositionChanged?.Invoke(this);

        if (!HasArrived && result.ReachedDestination)
        {
            HasArrived = true;
            Arrived?.Invoke(this);
        }
    }
}