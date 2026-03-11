using Godot;
using SurveillanceStategodot.scripts.domain.vision;
using SurveillanceStategodot.scripts.interaction;

namespace SurveillanceStategodot.scripts.presentation.vision;

/// <summary>
/// Observes WorldState.VisionSourceAdded / VisionSourceRemoved and spawns
/// a VisionSourceVisual for each map-visible VisionSource.
/// </summary>
public partial class VisionPresentationRoot : Node
{
    [Export] private SimulationController _simulationController = null!;
    [Export] private PackedScene _visionSourceVisualScene = null!;

    public override void _Ready()
    {
        _simulationController.World.VisionSourceAdded += OnVisionSourceAdded;
        _simulationController.World.VisionSourceRemoved += OnVisionSourceRemoved;
    }

    public override void _ExitTree()
    {
        _simulationController.World.VisionSourceAdded -= OnVisionSourceAdded;
        _simulationController.World.VisionSourceRemoved -= OnVisionSourceRemoved;
    }

    private void OnVisionSourceAdded(VisionSource source)
    {
        if (!source.IsMapVisible)
            return;

        var visual = _visionSourceVisualScene.Instantiate<VisionSourceVisual>();
        visual.SetVisionSource(source);
        AddChild(visual);
    }

    private void OnVisionSourceRemoved(VisionSource source)
    {
        // VisionSource.Deactivated (fired by WorldState.RemoveVisionSource) triggers
        // QueueFree inside VisionSourceVisual, so nothing extra to do here.
    }
}

