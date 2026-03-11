using Godot;
using SurveillanceStategodot.scripts.domain.vision;

namespace SurveillanceStategodot.scripts.presentation.vision;

/// <summary>
/// Renders a map-visible disc for one VisionSource.
/// Subscribes to VisionSource.Changed and VisionSource.Deactivated directly.
/// Spawned by VisionPresentationRoot; call SetVisionSource immediately after AddChild.
/// </summary>
public partial class VisionSourceVisual : Node3D
{
    [Signal]
    public delegate void RangeChangedEventHandler(float newRange);

    private VisionSource? _source;

    private float _range;
    private float Range
    {
        set
        {
            if(value == _range) return;
            _range = value;
            EmitSignalRangeChanged(value);
        }
    }

    public void SetVisionSource(VisionSource source)
    {
        UnsubscribeFromSource();
        _source = source;
        SubscribeToSource();
        SyncFromSource(source);
    }

    public override void _ExitTree()
    {
        UnsubscribeFromSource();
        base._ExitTree();
    }

    private void SubscribeToSource()
    {
        if (_source == null) return;
        _source.Changed += OnSourceChanged;
        _source.Deactivated += OnSourceDeactivated;
    }

    private void UnsubscribeFromSource()
    {
        if (_source == null) return;
        _source.Changed -= OnSourceChanged;
        _source.Deactivated -= OnSourceDeactivated;
    }

    private void OnSourceChanged(VisionSource source) => SyncFromSource(source);

    private void OnSourceDeactivated(VisionSource source)
    {
        UnsubscribeFromSource();
        _source = null;
        QueueFree();
    }

    private void SyncFromSource(VisionSource source)
    {
        GlobalPosition = source.WorldPosition;
        Range = source.Range;
    }
}


