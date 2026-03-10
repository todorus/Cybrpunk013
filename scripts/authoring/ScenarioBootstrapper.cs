using Godot;
using SurveillanceStategodot.scripts.domain;
using SurveillanceStategodot.scripts.domain.plot;
using SurveillanceStategodot.scripts.interaction;
using SurveillanceStategodot.scripts.presentation.sites;
using SurveillanceStategodot.scripts.util;

namespace SurveillanceStategodot.scripts.authoring;

public partial class ScenarioBootstrapper : Node
{
    [Export]
    private SimulationController _simulationController;
    
    [Export]
    private PlotResource[] _plotDefinitions = [];

    public override void _Ready()
    {
        base._Ready();
        
        GetTree().Root.FindAllChildrenOfType<SiteNode>()
            .ForEach(siteNode => siteNode.SimulationController = _simulationController);
    }
    
    private void InitializePlots()
    {
        var world = _simulationController.World;
        foreach (var plotDefinition in _plotDefinitions)
        {
            var plot = plotDefinition.ToPlot();
            world.RegisterPlot(plot);
        }
    }
}