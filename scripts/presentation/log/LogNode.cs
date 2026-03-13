using Godot;
using SurveillanceStategodot.scripts.domain.observation;
using SurveillanceStategodot.scripts.interaction;
using SurveillanceStategodot.scripts.presentation.portrait;

namespace SurveillanceStategodot.scripts.presentation.log;

public partial class LogNode : Container
{
    [Export]
    private SimulationController _simulationController;
    
    [Export]
    private PortraitCache _portraitCache;
    
    [Export]
    private ResourceRegistry _resourceRegistry;
    
    [Export]
    private PackedScene _LogEntryScene;

    // One LogEntryNode per unique ObservationLogKey.
    private readonly System.Collections.Generic.Dictionary<ObservationLogKey, LogEntryNode> _entryNodes = new();

    public override void _Ready()
    {
        _simulationController.ObservationLog.EntryAdded += OnEntryAdded;
        _simulationController.ObservationLog.EntryUpdated += OnEntryUpdated;
    }

    public override void _ExitTree()
    {
        _simulationController.ObservationLog.EntryAdded -= OnEntryAdded;
        _simulationController.ObservationLog.EntryUpdated -= OnEntryUpdated;
    }

    private void OnEntryAdded(AggregatedObservationLogEntry entry)
    {
        var node = _LogEntryScene.Instantiate<LogEntryNode>();
        node.SetEntry(entry, _simulationController.World, _portraitCache, _resourceRegistry);
        _entryNodes[entry.Key] = node;
        AddChild(node);
        // Most recent entry at the top.
        if (GetChildCount() > 1)
            MoveChild(node, 0);
    }

    private void OnEntryUpdated(AggregatedObservationLogEntry entry)
    {
        if (_entryNodes.TryGetValue(entry.Key, out var node))
        {
            node.RefreshEntry(entry);
            // Move updated entry to the top.
            MoveChild(node, 0);
        }
    }
}