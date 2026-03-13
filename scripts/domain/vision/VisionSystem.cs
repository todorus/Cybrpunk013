using System;
using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.observation;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.vision;

public sealed class VisionSystem : ISimulationSystem
{
    private readonly float _operatorVisionRange;

    private WorldState _world = null!;
    private SimulationEventBus _eventBus = null!;

    // Local movement->visionSourceId index so we can clean up on arrival.
    private readonly Dictionary<string, string> _visionSourceIdByMovementId = new();

    // Tracks which (sourceId, characterId) pairs are currently inside range.
    // An observation fires once on entry; nothing fires again until the character
    // leaves and re-enters the range.
    private readonly HashSet<(string sourceId, string characterId)> _inRange = new();

    public VisionSystem(float operatorVisionRange)
    {
        _operatorVisionRange = operatorVisionRange;
    }

    public void Initialize(WorldState world, SimulationEventBus eventBus)
    {
        _world = world;
        _eventBus = eventBus;

        _eventBus.Subscribe<MovementStartedEvent>(OnMovementStarted);
        _eventBus.Subscribe<MovementArrivedEvent>(OnMovementArrived);
        _eventBus.Subscribe<CharacterEnteredSiteEvent>(OnCharacterEnteredSite);
        _eventBus.Subscribe<CharacterExitedSiteEvent>(OnCharacterExitedSite);
        _eventBus.Subscribe<OperationStartedEvent>(OnOperationStarted);
        _eventBus.Subscribe<OperationCompletedEvent>(OnOperationCompleted);
    }

    public void Tick(double delta)
    {
        foreach (var source in _world.VisionSources.Values)
        {
            ScanMovingNpcsForSource(source);
        }
    }

    // ── Movement-based vision source ─────────────────────────────────────────

    private void OnMovementStarted(MovementStartedEvent evt)
    {
        var character = evt.Movement.Character;
        if (character == null || !character.IsOperator)
            return;

        var sourceId = $"movement:{evt.Movement.Id}";
        var source = new VisionSource(
            id: sourceId,
            owner: character,
            type: VisionSourceType.MovingOperator,
            range: _operatorVisionRange,
            isMapVisible: true);

        source.SetWorldPosition(character.Position.WorldPosition);

        _visionSourceIdByMovementId[evt.Movement.Id] = sourceId;
        character.Position.Changed += OnCharacterPositionChanged;

        _world.RegisterVisionSource(source);
    }

    private void OnMovementArrived(MovementArrivedEvent evt)
    {
        if (!_visionSourceIdByMovementId.TryGetValue(evt.Movement.Id, out var sourceId))
            return;

        _visionSourceIdByMovementId.Remove(evt.Movement.Id);

        if (evt.Movement.Character != null)
            evt.Movement.Character.Position.Changed -= OnCharacterPositionChanged;

        // Clear any in-range state for this source so re-entry fires fresh next time.
        _inRange.RemoveWhere(pair => pair.sourceId == sourceId);

        _world.RemoveVisionSource(sourceId);
    }

    private void OnCharacterPositionChanged(CharacterPosition position)
    {
        // Find the movement whose character owns this position.
        foreach (var (movementId, sourceId) in _visionSourceIdByMovementId)
        {
            if (!_world.TryGetVisionSource(sourceId, out var source) || source == null)
                continue;

            if (source.Owner?.Position != position)
                continue;

            source.SetWorldPosition(position.WorldPosition);
            ScanMovingNpcsForSource(source);
            return;
        }
    }

    // ── Shared range scan ────────────────────────────────────────────────────

    /// <summary>
    /// Checks every moving NPC against the given source.
    /// Fires SpottedMoving once on range entry; clears state on range exit.
    /// </summary>
    private void ScanMovingNpcsForSource(VisionSource source)
    {
        foreach (var candidate in _world.Characters)
        {
            if (candidate.IsOperator || candidate.CurrentMovement == null)
                continue;

            // Operator-owned sources never self-observe.
            if (candidate == source.Owner)
                continue;

            var key = (source.Id, candidate.Id);
            bool nowInRange = source.WorldPosition.DistanceTo(
                candidate.Position.WorldPosition) <= source.Range;

            if (nowInRange)
            {
                if (_inRange.Add(key))
                {
                    // Freshly entered range — fire observation.
                    PublishObservation(
                        site: null,
                        character: candidate,
                        operation: null,
                        observationType: ObservationType.SpottedMoving);
                }
            }
            else
            {
                // Left range — clear so re-entry fires again later.
                _inRange.Remove(key);
            }
        }
    }

    // ── Site enter / exit detection ──────────────────────────────────────────

    private void OnCharacterEnteredSite(CharacterEnteredSiteEvent evt)
    {
        if (evt.Character.IsOperator)
            return;

        foreach (var source in _world.VisionSources.Values)
        {
            var key = (source.Id, evt.Character.Id);

            // Only log if the character was already tracked as in-range while moving.
            if (!_inRange.Contains(key))
                continue;

            if (source.WorldPosition.DistanceTo(evt.Site.EntryPosition) > source.Range)
                continue;

            PublishObservation(
                site: evt.Site,
                character: evt.Character,
                operation: evt.CurrentOperation,
                observationType: ObservationType.EnteredSite);
        }
    }

    private void OnCharacterExitedSite(CharacterExitedSiteEvent evt)
    {
        if (evt.Character.IsOperator)
            return;

        foreach (var source in _world.VisionSources.Values)
        {
            var key = (source.Id, evt.Character.Id);

            if (source.WorldPosition.DistanceTo(evt.Site.EntryPosition) > source.Range)
            {
                // Exit happened outside vision — clear stale in-range state so
                // re-entry into range later fires SpottedMoving fresh.
                _inRange.Remove(key);
                continue;
            }

            // Exit is within vision range — log it. Keep the key in _inRange so
            // ScanMovingNpcsForSource does not immediately re-fire SpottedMoving
            // for the movement that starts right after the exit.
            PublishObservation(
                site: evt.Site,
                character: evt.Character,
                operation: evt.CurrentOperation,
                observationType: ObservationType.ExitedSite);

            _inRange.Add(key);
        }
    }

    // ── Stakeout / watch-site operation vision source ─────────────────────────

    private void OnOperationStarted(OperationStartedEvent evt)
    {
        var operation = evt.Operation;
        if (operation.VisionType != OperationVisionType.Stakeout)
            return;

        Character? owner = operation.Participants.Count > 0 ? operation.Participants[0] : null;
        if (owner == null || !owner.IsOperator)
            return;

        // Decide source type: a tail-assignment hold uses WatchSite; a normal stakeout uses StakeoutPost.
        var sourceType = VisionSourceType.StakeoutPost;
        if (_world.TryGetAssignmentByOperationId(operation.Id, out var assignment) &&
            assignment.Kind == SurveillanceStategodot.scripts.domain.assignment.AssignmentKind.TailCharacter)
        {
            sourceType = VisionSourceType.WatchSite;
        }

        var source = new VisionSource(
            id: $"operation:{operation.Id}",
            owner: owner,
            type: sourceType,
            range: _operatorVisionRange,
            isMapVisible: true);

        // Position at the operator's current nav-graph position, not a site entry point.
        source.SetWorldPosition(owner.Position.WorldPosition);

        if (operation.SiteContext != null)
            source.SetSiteContext(operation.SiteContext);

        _world.RegisterVisionSource(source);
    }

    private void OnOperationCompleted(OperationCompletedEvent evt)
    {
        var sourceId = $"operation:{evt.Operation.Id}";
        _inRange.RemoveWhere(pair => pair.sourceId == sourceId);
        _world.RemoveVisionSource(sourceId);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void PublishObservation(
        Site? site,
        Character? character,
        Operation? operation,
        ObservationType observationType)
    {
        var observation = new Observation(
            id: Guid.NewGuid().ToString(),
            siteId: site?.Id,
            characterId: character?.Id,
            operationId: operation?.Id,
            time: _world.Time,
            observationType: observationType,
            siteLabelSnapshot: site?.Label,
            characterLabelSnapshot: character?.DisplayName,
            operationLabelSnapshot: operation?.Label);

        _eventBus.Publish(new ObservationCreatedEvent(observation, _world.Time));
    }
}