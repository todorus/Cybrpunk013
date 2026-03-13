using System.Collections.Generic;
using System.Threading;
using Godot;
using SurveillanceStategodot.scripts.domain;
using SurveillanceStategodot.scripts.domain.assignment;
using SurveillanceStategodot.scripts.interaction;
using SurveillanceStategodot.scripts.presentation.portrait;
using SurveillanceStategodot.scripts.util;

namespace SurveillanceStategodot.scripts.presentation.operators;

public partial class OperatorList : Container
{
    [Export] private PortraitCache _portraitCache = null!;

    [Export] private PackedScene _operatorScene;

    [Export] private ResourceRegistry _resourceRegistry;

    [Export] private SimulationController _simulationController;

    private CancellationTokenSource _cts = new();
    private readonly Dictionary<string, OperatorDisplay> _displaysByCharacterId = new();

    public override void _Ready()
    {
        base._Ready();
        _simulationController.World.OnOperatorsChanged += Refresh;
        _simulationController.EventBus.Subscribe<AssignmentCreatedEvent>(OnAssignmentCreated);
        _simulationController.EventBus.Subscribe<AssignmentCompletedEvent>(OnAssignmentEnded);
        _simulationController.EventBus.Subscribe<AssignmentCancelledEvent>(OnAssignmentEnded);
        Refresh(_simulationController.World.Operators);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (_simulationController?.World != null)
            _simulationController.World.OnOperatorsChanged -= Refresh;

        if (_simulationController?.EventBus != null)
        {
            _simulationController.EventBus.Unsubscribe<AssignmentCreatedEvent>(OnAssignmentCreated);
            _simulationController.EventBus.Unsubscribe<AssignmentCompletedEvent>(OnAssignmentEnded);
            _simulationController.EventBus.Unsubscribe<AssignmentCancelledEvent>(OnAssignmentEnded);
        }
    }

    private void OnAssignmentCreated(AssignmentCreatedEvent evt)
    {
        var characterId = evt.Assignment.Character?.Id;
        if (characterId == null) return;
        if (_displaysByCharacterId.TryGetValue(characterId, out var display))
            display.SetAssignment(evt.Assignment);
    }

    private void OnAssignmentEnded(AssignmentCompletedEvent evt)
    {
        var characterId = evt.Assignment.Character?.Id;
        if (characterId == null) return;
        if (_displaysByCharacterId.TryGetValue(characterId, out var display))
            display.SetAssignment(null);
    }

    private void OnAssignmentEnded(AssignmentCancelledEvent evt)
    {
        var characterId = evt.Assignment.Character?.Id;
        if (characterId == null) return;
        if (_displaysByCharacterId.TryGetValue(characterId, out var display))
            display.SetAssignment(null);
    }

    private void OnRecallPressed(string characterId)
    {
        var assignment = _simulationController.World.GetActiveAssignmentForCharacter(characterId);
        if (assignment == null) return;

        _simulationController.EventBus.Publish(
            new AssignmentCancelRequestedEvent(assignment, _simulationController.World.Time));
    }

    private void Refresh(List<Character> operators)
    {
        _cts.Cancel();
        _cts.Dispose();
        _cts = new CancellationTokenSource();
        RefreshAsync(operators, _cts.Token);
    }

    private async void RefreshAsync(List<Character> operators, CancellationToken ct)
    {
        Cleanup();

        foreach (var operatorCharacter in operators)
        {
            if (ct.IsCancellationRequested) return;

            if (!_resourceRegistry.TryGetCharacter(operatorCharacter.Id, out var operatorResource)) continue;

            var avatar = await _portraitCache.GetOrRenderAsync(operatorResource);

            if (ct.IsCancellationRequested) return;

            var operatorDisplay = _operatorScene.Instantiate<OperatorDisplay>();
            operatorDisplay.Avatar = avatar;
            operatorDisplay.Character = operatorCharacter;
            operatorDisplay.SetAssignment(null);
            operatorDisplay.RecallPressed += OnRecallPressed;

            _displaysByCharacterId[operatorCharacter.Id] = operatorDisplay;
            AddChild(operatorDisplay);
        }
    }

    private void Cleanup()
    {
        _displaysByCharacterId.Clear();
        this.ClearChildren();
    }
}