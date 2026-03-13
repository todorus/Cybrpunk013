using System;
using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.observation;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.vision;

public sealed class VisionSystem : ISimulationSystem
{
    private WorldState _world = null!;
    private SimulationEventBus _eventBus = null!;

    // operator character id → vision source id
    private readonly Dictionary<string, string> _visionSourceIdByOperatorId = new();

    // Global set of NPC character IDs currently within any vision source's range while moving.
    private readonly HashSet<string> _inRange = new();

    // Global set of (siteId, characterId) combos already reported as SpottedAtSite.
    private readonly HashSet<(string siteId, string characterId)> _seenAtSite = new();

    public VisionSystem() { }

    public void Initialize(WorldState world, SimulationEventBus eventBus)
    {
        _world = world;
        _eventBus = eventBus;

        _eventBus.Subscribe<CharacterLocationChangedEvent>(OnCharacterLocationChanged);
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

    // ── Vision source lifecycle ───────────────────────────────────────────────

    private void OnCharacterLocationChanged(CharacterLocationChangedEvent evt)
    {
        if (!evt.Character.IsOperator) return;

        if (evt.NewLocation == CharacterLocationType.NavGraph)
            EnsureVisionSource(evt.Character);
        else
            RemoveVisionSource(evt.Character);
    }

    private void EnsureVisionSource(Character character)
    {
        if (_visionSourceIdByOperatorId.ContainsKey(character.Id)) return;

        var sourceId = $"operator:{character.Id}";
        var source = new VisionSource(
            id: sourceId,
            owner: character,
            type: VisionSourceType.OperatorPresence,
            range: character.VisionRange,
            isMapVisible: true);

        source.SetWorldPosition(character.Position.WorldPosition);

        _visionSourceIdByOperatorId[character.Id] = sourceId;
        character.Position.Changed += OnOperatorPositionChanged;

        _world.RegisterVisionSource(source);
    }

    private void RemoveVisionSource(Character character)
    {
        if (!_visionSourceIdByOperatorId.TryGetValue(character.Id, out var sourceId)) return;

        _visionSourceIdByOperatorId.Remove(character.Id);
        character.Position.Changed -= OnOperatorPositionChanged;

        _world.RemoveVisionSource(sourceId);
        RebuildInRange();
    }

    private void OnOperatorPositionChanged(CharacterPosition position)
    {
        foreach (var (operatorId, sourceId) in _visionSourceIdByOperatorId)
        {
            if (!_world.TryGetVisionSource(sourceId, out var source) || source == null) continue;
            if (source.Owner?.Position != position) continue;
            source.SetWorldPosition(position.WorldPosition);
            return;
        }
    }

    // ── Moving NPC scan ───────────────────────────────────────────────────────

    private void ScanMovingNpcs()
    {
        var nowInRange = new HashSet<string>();

        foreach (var source in _world.VisionSources.Values)
        {
            foreach (var candidate in _world.Characters)
            {
                if (candidate.IsOperator || candidate.CurrentMovement == null) continue;
                if (candidate == source.Owner) continue;

                if (source.WorldPosition.DistanceTo(candidate.Position.WorldPosition) <= source.Range)
                    nowInRange.Add(candidate.Id);
            }
        }

        foreach (var characterId in nowInRange)
        {
            if (_inRange.Add(characterId))
            {
                var character = _world.GetCharacter(characterId);
                if (character == null) continue;
                var source = FindBestSourceForCharacter(character);
                if (source == null) continue;
                PublishObservation(source, null, character, null, ObservationType.SpottedMoving);
            }
        }

        _inRange.RemoveWhere(id => !nowInRange.Contains(id));
    }

    private VisionSource? FindBestSourceForCharacter(Character character)
    {
        foreach (var source in _world.VisionSources.Values)
        {
            if (source.WorldPosition.DistanceTo(character.Position.WorldPosition) <= source.Range)
                return source;
        }
        return null;
    }

    private void RebuildInRange()
    {
        _inRange.Clear();
        foreach (var source in _world.VisionSources.Values)
        {
            foreach (var candidate in _world.Characters)
            {
                if (candidate.IsOperator || candidate.CurrentMovement == null) continue;
                if (candidate == source.Owner) continue;
                if (source.WorldPosition.DistanceTo(candidate.Position.WorldPosition) <= source.Range)
                    _inRange.Add(candidate.Id);
            }
        }
    }

    // ── Site scan ─────────────────────────────────────────────────────────────

    private void ScanSitesForSource(VisionSource source)
    {
        foreach (var site in _world.SitesById.Values)
        {
            if (source.WorldPosition.DistanceTo(site.EntryPosition) > source.Range)
                continue;

            var npcsAtSite = new HashSet<Character>();

            if (source.Type.CanSeeOccupants())
                foreach (var occupant in site.Occupants)
                    if (!occupant.IsOperator) npcsAtSite.Add(occupant);

            if (source.Type.CanSeeOperations())
                foreach (var operation in site.ActiveOperations)
                    foreach (var participant in operation.Participants)
                        if (!participant.IsOperator) npcsAtSite.Add(participant);

            foreach (var npc in npcsAtSite)
            {
                var seenKey = (site.Id, npc.Id);
                if (!_seenAtSite.Add(seenKey)) continue;

                Operation? activeOperation = null;
                if (source.Type.CanSeeOperations())
                {
                    foreach (var op in site.ActiveOperations)
                    {
                        if (op.Participants.Contains(npc)) { activeOperation = op; break; }
                    }
                }

                PublishObservation(source, site, npc, activeOperation,
                    ObservationType.SpottedAtSite, ResolveCompliance(source, activeOperation));
            }
        }

        // Clear stale (siteId, characterId) pairs where no source covers the site
        // or the character has left the site.
        _seenAtSite.RemoveWhere(key =>
        {
            if (!_world.SitesById.TryGetValue(key.siteId, out var s)) return true;

            bool anySiteInRange = false;
            foreach (var src in _world.VisionSources.Values)
                if (src.WorldPosition.DistanceTo(s.EntryPosition) <= src.Range)
                { anySiteInRange = true; break; }

            if (!anySiteInRange) return true;

            foreach (var occ in s.Occupants)
                if (occ.Id == key.characterId) return false;

            return true;
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void PublishObservation(
        VisionSource source, Site? site, Character? character, Operation? operation,
        ObservationType observationType, ComplianceType? complianceOverride = null)
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
        if (operation == null) return ComplianceType.Compliant;
        if (source.Type.CanDetectNonCompliance()) return operation.ComplianceType;
        return operation.ComplianceType == ComplianceType.NonCompliant
            ? ComplianceType.Suspicious
            : operation.ComplianceType;
    }
}
