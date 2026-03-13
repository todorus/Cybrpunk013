using System;
using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.observation;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.vision;

public sealed class VisionSystem : ISimulationSystem
{
    private WorldState _world = null!;
    private SimulationEventBus _eventBus = null!;

    // Local movement->visionSourceId index so we can clean up on arrival.
    private readonly Dictionary<string, string> _visionSourceIdByMovementId = new();

    // Global set of NPC character IDs currently within any vision source's range while moving.
    // An observation fires once when a character enters range across all sources.
    // Cleared when no source can see the character anymore.
    private readonly HashSet<string> _inRange = new();

    // Global set of (siteId, characterId) combos already reported as SpottedAtSite.
    // Fires once when first seen at a site; clears when the character leaves the site.
    private readonly HashSet<(string siteId, string characterId)> _seenAtSite = new();


    public VisionSystem()
    {
    }

    public void Initialize(WorldState world, SimulationEventBus eventBus)
    {
        _world = world;
        _eventBus = eventBus;

        _eventBus.Subscribe<MovementStartedEvent>(OnMovementStarted);
        _eventBus.Subscribe<MovementArrivedEvent>(OnMovementArrived);
        _eventBus.Subscribe<OperationStartedEvent>(OnOperationStarted);
        _eventBus.Subscribe<OperationCompletedEvent>(OnOperationCompleted);
    }

    public void Tick(double delta)
    {
        ScanMovingNpcs();

        foreach (var source in _world.VisionSources.Values)
        {
            if (source.Type.CanSeeOperations() || source.Type.CanSeeOccupants())
                ScanSitesForSource(source);
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
            range: character.VisionRange,
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

        _world.RemoveVisionSource(sourceId);

        // After removing this source, re-evaluate which characters are still in range
        // so _inRange stays accurate.
        RebuildInRange();
    }

    private void OnCharacterPositionChanged(CharacterPosition position)
    {
        foreach (var (_, sourceId) in _visionSourceIdByMovementId)
        {
            if (!_world.TryGetVisionSource(sourceId, out var source) || source == null)
                continue;

            if (source.Owner?.Position != position)
                continue;

            source.SetWorldPosition(position.WorldPosition);
            return;
        }
    }

    // ── Moving NPC scan ──────────────────────────────────────────────────────

    /// <summary>
    /// Checks every moving NPC against all sources.
    /// Fires SpottedMoving once when a character first enters any source's range.
    /// Clears the character from _inRange only when no source can see them.
    /// </summary>
    private void ScanMovingNpcs()
    {
        var nowInRange = new HashSet<string>();

        foreach (var source in _world.VisionSources.Values)
        {
            foreach (var candidate in _world.Characters)
            {
                if (candidate.IsOperator || candidate.CurrentMovement == null)
                    continue;

                if (candidate == source.Owner)
                    continue;

                bool inRange = source.WorldPosition.DistanceTo(
                    candidate.Position.WorldPosition) <= source.Range;

                if (inRange)
                    nowInRange.Add(candidate.Id);
            }
        }

        // Fire SpottedMoving for newly seen characters.
        foreach (var characterId in nowInRange)
        {
            if (_inRange.Add(characterId))
            {
                var character = _world.GetCharacter(characterId);
                if (character == null) continue;

                // Find the best source that can see this character to attribute the observation.
                var source = FindBestSourceForCharacter(character);
                if (source == null) continue;

                PublishObservation(
                    source: source,
                    site: null,
                    character: character,
                    operation: null,
                    observationType: ObservationType.SpottedMoving);
            }
        }

        // Remove characters that are no longer in any source's range.
        _inRange.RemoveWhere(id => !nowInRange.Contains(id));
    }

    /// <summary>
    /// Returns the first vision source that can see the given character, preferring
    /// sources owned by an operator. Used to attribute an observation to a source.
    /// </summary>
    private VisionSource? FindBestSourceForCharacter(Character character)
    {
        foreach (var source in _world.VisionSources.Values)
        {
            if (source.WorldPosition.DistanceTo(character.Position.WorldPosition) <= source.Range)
                return source;
        }
        return null;
    }

    /// <summary>
    /// After a source is removed, rebuild _inRange from scratch so stale entries are cleared.
    /// </summary>
    private void RebuildInRange()
    {
        _inRange.Clear();
        foreach (var source in _world.VisionSources.Values)
        {
            foreach (var candidate in _world.Characters)
            {
                if (candidate.IsOperator || candidate.CurrentMovement == null)
                    continue;

                if (candidate == source.Owner)
                    continue;

                bool inRange = source.WorldPosition.DistanceTo(
                    candidate.Position.WorldPosition) <= source.Range;

                if (inRange)
                    _inRange.Add(candidate.Id);
            }
        }
    }

    // ── Site scan for stakeout / watch sources ───────────────────────────────

    /// <summary>
    /// For sources that CanSeeOperations / CanSeeOccupants, fires SpottedAtSite once
    /// per (site, character) globally when first seen. Clears when the character leaves the site.
    /// </summary>
    private void ScanSitesForSource(VisionSource source)
    {
        foreach (var site in _world.SitesById.Values)
        {
            if (source.WorldPosition.DistanceTo(site.EntryPosition) > source.Range)
                continue;

            var npcsAtSite = new HashSet<Character>();

            if (source.Type.CanSeeOccupants())
            {
                foreach (var occupant in site.Occupants)
                    if (!occupant.IsOperator) npcsAtSite.Add(occupant);
            }

            if (source.Type.CanSeeOperations())
            {
                foreach (var operation in site.ActiveOperations)
                    foreach (var participant in operation.Participants)
                        if (!participant.IsOperator) npcsAtSite.Add(participant);
            }

            foreach (var npc in npcsAtSite)
            {
                var seenKey = (site.Id, npc.Id);
                if (!_seenAtSite.Add(seenKey))
                    continue; // Already reported this character at this site.

                Operation? activeOperation = null;
                if (source.Type.CanSeeOperations())
                {
                    foreach (var op in site.ActiveOperations)
                    {
                        if (op.Participants.Contains(npc))
                        {
                            activeOperation = op;
                            break;
                        }
                    }
                }

                var compliance = ResolveCompliance(source, activeOperation);
                PublishObservation(
                    source: source,
                    site: site,
                    character: npc,
                    operation: activeOperation,
                    observationType: ObservationType.SpottedAtSite,
                    complianceOverride: compliance);
            }
        }

        // Clear stale entries: only evict if no source covers the site AND the character is gone.
        _seenAtSite.RemoveWhere(key =>
        {
            if (!_world.SitesById.TryGetValue(key.siteId, out var s))
                return true;

            // Check if any source still covers this site.
            bool anySiteInRange = false;
            foreach (var src in _world.VisionSources.Values)
            {
                if (src.WorldPosition.DistanceTo(s.EntryPosition) <= src.Range)
                {
                    anySiteInRange = true;
                    break;
                }
            }

            if (!anySiteInRange)
                return true;

            // Site is still in range — keep if character is still an occupant.
            foreach (var occ in s.Occupants)
                if (occ.Id == key.characterId) return false;

            return true;
        });
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
            range: owner.VisionRange,
            isMapVisible: true);

        source.SetWorldPosition(owner.Position.WorldPosition);

        if (operation.SiteContext != null)
            source.SetSiteContext(operation.SiteContext);

        _world.RegisterVisionSource(source);
    }

    private void OnOperationCompleted(OperationCompletedEvent evt)
    {
        var sourceId = $"operation:{evt.Operation.Id}";
        _world.RemoveVisionSource(sourceId);

        // Rebuild _inRange since a source was removed.
        RebuildInRange();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void PublishObservation(
        VisionSource source,
        Site? site,
        Character? character,
        Operation? operation,
        ObservationType observationType,
        ComplianceType? complianceOverride = null)
    {
        var compliance = complianceOverride ?? ResolveCompliance(source, operation);

        var observation = new Observation(
            id: Guid.NewGuid().ToString(),
            siteId: site?.Id,
            characterId: character?.Id,
            operationId: operation?.Id,
            time: _world.Time,
            observationType: observationType,
            complianceType: compliance,
            siteLabelSnapshot: site?.Label,
            characterLabelSnapshot: character?.DisplayName,
            operationLabelSnapshot: operation?.Label);

        _eventBus.Publish(new ObservationCreatedEvent(observation, _world.Time));
    }

    private static ComplianceType ResolveCompliance(VisionSource source, Operation? operation)
    {
        if (operation == null)
            return ComplianceType.Compliant;

        if (source.Type.CanDetectNonCompliance())
            return operation.ComplianceType;

        return operation.ComplianceType == ComplianceType.NonCompliant
            ? ComplianceType.Suspicious
            : operation.ComplianceType;
    }
}

