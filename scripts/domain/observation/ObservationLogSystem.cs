using System;
using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.observation;

public sealed class ObservationLogSystem : ISimulationSystem
{
    private readonly Dictionary<ObservationLogKey, AggregatedObservationLogEntry> _entries = new();

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
    }

    private void OnObservationCreated(ObservationCreatedEvent evt)
    {
        var obs = evt.Observation;

        var key = new ObservationLogKey(
            obs.SiteId,
            obs.CharacterId,
            ActivityId: null,       // not aggregated by operation
            obs.ObservationType);

        if (_entries.TryGetValue(key, out var existing))
        {
            existing.AddOccurrence(obs.Time);
            EntryUpdated?.Invoke(existing);
            return;
        }

        var entry = new AggregatedObservationLogEntry(
            key,
            siteLabel: obs.SiteLabelSnapshot ?? "Unknown Site",
            characterLabel: obs.CharacterLabelSnapshot ?? "Unknown Character",
            operationLabel: obs.OperationLabelSnapshot ?? "Unknown Operation",
            firstSeenTime: obs.Time);

        _entries.Add(key, entry);
        EntryAdded?.Invoke(entry);
    }
}