using System;
using Godot;

namespace SurveillanceStategodot.scripts.domain;

/// <summary>
/// Authoritative world-space position component for a Character.
/// Owned by the Character; updated by MovementSystem on each tick and set
/// explicitly when spawning a character at a known location.
/// Consumers (VisionSystem, AssignmentSystem, etc.) read from here rather than
/// from Movement.CurrentWorldPosition.
/// </summary>
public sealed class CharacterPosition
{
    public Vector3 WorldPosition { get; private set; }
    public Vector3 Forward { get; private set; } = Vector3.Forward;

    public event Action<CharacterPosition>? Changed;

    public CharacterPosition(Vector3 initialPosition)
    {
        WorldPosition = initialPosition;
    }

    /// <summary>Sets position and forward direction (called every movement tick).</summary>
    public void Update(Vector3 position, Vector3 forward)
    {
        WorldPosition = position;
        if (forward.LengthSquared() > 0.0001f)
            Forward = forward.Normalized();
        Changed?.Invoke(this);
    }

    /// <summary>Sets position only, keeping forward unchanged (used when spawning).</summary>
    public void Set(Vector3 position)
    {
        WorldPosition = position;
        Changed?.Invoke(this);
    }
}

