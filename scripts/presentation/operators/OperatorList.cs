using System.Collections.Generic;
using System.Threading;
using Godot;
using SurveillanceStategodot.scripts.domain;
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

    public override void _Ready()
    {
        base._Ready();
        _simulationController.World.OnOperatorsChanged += Refresh;
        Refresh(_simulationController.World.Operators);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (_simulationController?.World != null)
        {
            _simulationController.World.OnOperatorsChanged -= Refresh;
        }
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

            AddChild(operatorDisplay);
        }
    }

    private void Cleanup()
    {
        this.ClearChildren();
    }
}