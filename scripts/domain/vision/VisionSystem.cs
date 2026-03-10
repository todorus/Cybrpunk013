using System;
using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.observation;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.vision;

public sealed class VisionSystem : ISimulationSystem
{
    private const float DefaultOperatorVisionRange = 10f;
    private const string StakeoutOperationLabel = "Stakeout";

    private WorldState _world = null!;
    private SimulationEventBus _eventBus = null!;

    private readonly Dictionary<string, VisionSource> _movementVisionByMovementId = new();
    private readonly Dictionary<string, VisionSource> _operationVisionByOperationId = new();

    // Prevent spam from emitting "moving" observations every frame.
    private readonly Dictionary<(string sourceId, string targetCharacterId, string operationId), double> _lastObservationTimes = new();
    private const double MovingObservationCooldownSeconds = 5.0;

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
    }

    private void OnMovementStarted(MovementStartedEvent evt)
    {
        var character = evt.Movement.Character;
        if (character == null || !character.IsOperator)
            return;

        var source = new VisionSource(
            id: $"movement:{evt.Movement.Id}",
            owner: character,
            type: VisionSourceType.MovingOperator,
            range: DefaultOperatorVisionRange)
        {
            WorldPosition = evt.Movement.CurrentWorldPosition
        };

        _movementVisionByMovementId[evt.Movement.Id] = source;
        evt.Movement.PositionChanged += OnMovementPositionChanged;
    }

    private void OnMovementArrived(MovementArrivedEvent evt)
    {
        if (_movementVisionByMovementId.Remove(evt.Movement.Id))
        {
            evt.Movement.PositionChanged -= OnMovementPositionChanged;
        }
    }

    private void OnMovementPositionChanged(Movement movement)
    {
        if (!_movementVisionByMovementId.TryGetValue(movement.Id, out var source))
            return;

        source.WorldPosition = movement.CurrentWorldPosition;

        foreach (var candidate in _world.Characters)
        {
            if (candidate == movement.Character)
                continue;

            if (candidate.IsOperator)
                continue;

            if (candidate.CurrentMovement == null)
                continue;

            if (source.WorldPosition.DistanceTo(candidate.CurrentMovement.CurrentWorldPosition) > source.Range)
                continue;

            if (!PassesCooldown(source.Id, candidate.Id, null, MovingObservationCooldownSeconds))
                continue;

            PublishObservation(
                site: null,
                character: candidate,
                operation: null);
        }
    }

    private void OnCharacterEnteredSite(CharacterEnteredSiteEvent evt)
    {
        foreach (var source in EnumerateGraphVisionSources())
        {
            if (source.WorldPosition.DistanceTo(evt.Site.GlobalPosition) > source.Range)
                continue;

            PublishObservation(
                site: evt.Site,
                character: evt.Character,
                operation: evt.CurrentOperation);
        }
    }

    private void OnCharacterExitedSite(CharacterExitedSiteEvent evt)
    {
        foreach (var source in EnumerateGraphVisionSources())
        {
            if (source.WorldPosition.DistanceTo(evt.Site.GlobalPosition) > source.Range)
                continue;

            PublishObservation(
                site: evt.Site,
                character: evt.Character,
                operation: evt.CurrentOperation);
        }
    }

    private void OnOperationStarted(OperationStartedEvent evt)
    {
        var operation = evt.Operation;

        if (!string.Equals(operation.Label, StakeoutOperationLabel, StringComparison.OrdinalIgnoreCase))
            return;

        if (operation.SiteContext == null)
            return;

        Character? owner = operation.Participants.Count > 0 ? operation.Participants[0] : null;
        if (owner == null || !owner.IsOperator)
            return;

        var source = new VisionSource(
            id: $"operation:{operation.Id}",
            owner: owner,
            type: VisionSourceType.StakeoutPost,
            range: DefaultOperatorVisionRange)
        {
            SiteContext = operation.SiteContext,
            WorldPosition = operation.SiteContext.GlobalPosition
        };

        _operationVisionByOperationId[operation.Id] = source;
    }

    private void OnOperationCompleted(OperationCompletedEvent evt)
    {
        _operationVisionByOperationId.Remove(evt.Operation.Id);
    }

    private IEnumerable<VisionSource> EnumerateGraphVisionSources()
    {
        foreach (var source in _movementVisionByMovementId.Values)
            yield return source;

        foreach (var source in _operationVisionByOperationId.Values)
            yield return source;
    }

    private bool PassesCooldown(string sourceId, string targetCharacterId, string operationId, double cooldownSeconds)
    {
        var key = (sourceId, targetCharacterId, operationId);

        if (_lastObservationTimes.TryGetValue(key, out var lastTime))
        {
            if (_world.Time - lastTime < cooldownSeconds)
                return false;
        }

        _lastObservationTimes[key] = _world.Time;
        return true;
    }

    private void PublishObservation(
        Site? site,
        Character? character,
        Operation? operation
    )
    {
        var observation = new Observation(
            id: Guid.NewGuid().ToString(),
            siteId: site?.Id,
            characterId: character?.Id,
            operationId: operation?.Id,
            time: _world.Time,
            siteLabelSnapshot: site?.Label,
            characterLabelSnapshot: character?.DisplayName,
            operationLabelSnapshot: operation?.Label);

        _eventBus.Publish(new ObservationCreatedEvent(observation, _world.Time));
    }
}