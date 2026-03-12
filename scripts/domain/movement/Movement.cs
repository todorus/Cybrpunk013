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

    public MovementMode Mode { get; }

    /// <summary>For Pursuit mode: the character being followed.</summary>
    public Character? TargetCharacter { get; }

    public Vector3 CurrentWorldPosition { get; private set; }
    public Vector3 CurrentForward { get; private set; } = Vector3.Forward;

    /// <summary>
    /// True only for StaticPath movements when the end of the path is reached.
    /// Pursuit movements never self-arrive; AssignmentSystem cancels them externally.
    /// </summary>
    public bool HasArrived { get; private set; }

    public DispatchNavPath Path { get; private set; }
    public int SegmentIndex { get; private set; }

    public event Action<Movement>? PositionChanged;
    public event Action<Movement>? Arrived;

    // ── Static-path constructor ──────────────────────────────────────────────

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
        Mode = MovementMode.StaticPath;
        Path = path;
        CurrentWorldPosition = initialPosition;
    }

    // ── Pursuit constructor ──────────────────────────────────────────────────

    public Movement(
        string id,
        Character? character,
        Site? origin,
        Character targetCharacter,
        DispatchNavPath initialPath,
        Vector3 initialPosition)
    {
        Id = id;
        Character = character;
        Origin = origin;
        Destination = null;
        Mode = MovementMode.Pursuit;
        TargetCharacter = targetCharacter;
        Path = initialPath;
        CurrentWorldPosition = initialPosition;
    }

    // ── Path replacement (used by MovementSystem for pursuit repathing) ──────

    public void ReplacePath(DispatchNavPath newPath)
    {
        Path = newPath;
        SegmentIndex = 0;
        // Position stays where it is; next Advance() picks up from current world position.
    }

    // ── Advance ─────────────────────────────────────────────────────────────

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

        // Only StaticPath movements self-arrive.
        if (Mode == MovementMode.StaticPath && !HasArrived && result.ReachedDestination)
        {
            HasArrived = true;
            Arrived?.Invoke(this);
        }
    }

    /// <summary>
    /// Forces the movement to the arrived state.
    /// Used externally by AssignmentSystem to cancel pursuit once the target enters a site.
    /// </summary>
    public void ForceArrive()
    {
        if (HasArrived) return;
        HasArrived = true;
        Arrived?.Invoke(this);
    }
}

