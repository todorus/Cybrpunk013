using System;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.navigation.query;

namespace SurveillanceStategodot.scripts.domain.movement;

/// <summary>
/// State container representing the intent to move from one place to another.
/// Does not own position data — authoritative position lives in Character.Position.
/// Advancement logic lives in MovementSystem, which supplies and receives position externally.
/// </summary>
public sealed class Movement
{
    public string Id { get; }
    public Character? Character { get; }
    public Site? Origin { get; }
    public Site? Destination { get; }
    public MovementMode Mode { get; private set; }

    /// <summary>For Pursuit mode: the character being followed.</summary>
    public Character? TargetCharacter { get; }

    /// <summary>
    /// True only for StaticPath movements when the end of the path is reached.
    /// Pursuit movements never self-arrive; AssignmentSystem cancels them externally.
    /// </summary>
    public bool HasArrived { get; private set; }

    public DispatchNavPath Path { get; private set; }

    /// <summary>
    /// Current segment index into Path.WorldPoints.
    /// Maintained by MovementSystem during advancement.
    /// </summary>
    public int SegmentIndex { get; set; }

    public event Action<Movement>? Arrived;

    // ── Static-path constructor ──────────────────────────────────────────────

    public Movement(
        string id,
        Character? character,
        Site? origin,
        Site? destination,
        DispatchNavPath path)
    {
        Id = id;
        Character = character;
        Origin = origin;
        Destination = destination;
        Mode = MovementMode.StaticPath;
        Path = path;
    }

    // ── Pursuit constructor ──────────────────────────────────────────────────

    public Movement(
        string id,
        Character? character,
        Site? origin,
        Character targetCharacter,
        DispatchNavPath initialPath)
    {
        Id = id;
        Character = character;
        Origin = origin;
        Destination = null;
        Mode = MovementMode.Pursuit;
        TargetCharacter = targetCharacter;
        Path = initialPath;
    }

    // ── Path replacement (used by MovementSystem for pursuit repathing) ──────

    public void ReplacePath(DispatchNavPath newPath)
    {
        Path = newPath;
        SegmentIndex = 0;
    }

    // ── Arrival ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Marks the movement as arrived and fires the Arrived event.
    /// Called by MovementSystem when the path end is reached, or externally via ForceArrive.
    /// </summary>
    public void MarkArrived()
    {
        if (HasArrived) return;
        HasArrived = true;
        Arrived?.Invoke(this);
    }

    /// <summary>
    /// Forces the movement to the arrived state.
    /// Used by AssignmentSystem to cancel pursuit once the target enters a site.
    /// </summary>
    public void ForceArrive() => MarkArrived();

    /// <summary>
    /// Converts a Pursuit movement into a StaticPath movement so it finishes
    /// traveling to its current path end and then self-arrives.
    /// Used when the target becomes stationary and the operator should close on
    /// the last-known position rather than repathing indefinitely.
    /// </summary>
    public void ConvertToStaticPath()
    {
        Mode = MovementMode.StaticPath;
    }
}
