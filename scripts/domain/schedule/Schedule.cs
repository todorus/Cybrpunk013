using System.Collections.Generic;

namespace SurveillanceStategodot.scripts.domain.schedule;

/// <summary>
/// Baseline looping routine for an NPC character.
/// ScheduleSystem reads this to generate assignments when the character is idle.
/// </summary>
public sealed class Schedule
{
    public IReadOnlyList<ScheduleEntry> Entries { get; }

    private int _currentIndex;

    public bool HasEntries => Entries.Count > 0;

    public Schedule(IReadOnlyList<ScheduleEntry> entries)
    {
        Entries = entries;
        _currentIndex = 0;
    }

    /// <summary>
    /// Returns the current entry and advances the index, looping back to 0.
    /// </summary>
    public ScheduleEntry Advance()
    {
        var entry = Entries[_currentIndex];
        _currentIndex = (_currentIndex + 1) % Entries.Count;
        return entry;
    }

    /// <summary>Current entry without advancing the index.</summary>
    public ScheduleEntry Current => Entries[_currentIndex];
}

