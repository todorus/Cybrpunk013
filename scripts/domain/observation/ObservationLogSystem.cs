using System;
using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.observation;

public sealed class ObservationLogSystem : ISimulationSystem
{
    private readonly Dictionary<ObservationLogKey, AggregatedObservationLogEntry> _entries = new();

    // Observations collected this tick, deduplicated before being committed to the log.
    private readonly List<Observation> _pending = new();

    private SimulationEventBus _eventBus = null!;

    public IReadOnlyDictionary<ObservationLogKey, AggregatedObservationLogEntry> Entries => _entries;

    public event Action<AggregatedObservationLogEntry>? EntryAdded;
    public event Action<AggregatedObservationLogEntry>? EntryUpdated;

    public void Initialize(WorldState world, SimulationEventBus eventBus)
    {
        _eventBus = eventBus;
        _eventBus.Subscribe<ObservationCreatedEvent>(OnObservationCreated);
    }

    public void Tick(double delta)
    {
        if (_pending.Count == 0)
            return;

        // Deduplicate pending observations by log key, escalating compliance.
        var deduped = new Dictionary<ObservationLogKey, Observation>();
        foreach (var obs in _pending)
        {
            var key = MakeKey(obs);
            if (deduped.TryGetValue(key, out var existing))
            {
                // Keep the observation with the worst compliance level.
                if (obs.ComplianceType > existing.ComplianceType)
                    deduped[key] = obs;
            }
            else
            {
                deduped[key] = obs;
            }
        }

        _pending.Clear();

        // Commit deduplicated observations to the log.
        foreach (var (key, obs) in deduped)
        {
            if (_entries.TryGetValue(key, out var existing))
            {
                existing.AddOccurrence(obs.Time, obs.ComplianceType);
                EntryUpdated?.Invoke(existing);
            }
            else
            {
                var entry = new AggregatedObservationLogEntry(
                    key,
                    siteLabel: obs.SiteLabelSnapshot ?? "Unknown Site",
                    characterLabel: obs.CharacterLabelSnapshot ?? "Unknown Character",
                    operationLabel: obs.OperationLabelSnapshot ?? "Unknown Operation",
                    firstSeenTime: obs.Time,
                    complianceType: obs.ComplianceType);

                _entries.Add(key, entry);
                EntryAdded?.Invoke(entry);
            }
        }
    }

    private void OnObservationCreated(ObservationCreatedEvent evt)
    {
        _pending.Add(evt.Observation);
    }

    private static ObservationLogKey MakeKey(Observation obs) =>
        new(
            obs.SiteId,
            obs.CharacterId,
            obs.ObservationType);
}