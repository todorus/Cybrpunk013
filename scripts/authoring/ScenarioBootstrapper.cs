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
    private PlotResource[] _plotDefinitions = [];

    public void Init(SimulationController simulationController)
    {
        InitializeSites(simulationController);
        InitializePlots(simulationController);
    }

    private void InitializeSites(SimulationController simulationController)
    {
        GetTree().Root.FindAllChildrenOfType<SiteNode>()
            .ForEach(siteNode => siteNode.SimulationController = simulationController);
    }

    private void InitializePlots(SimulationController simulationController)
    {
        var world = simulationController.World;
        foreach (var plotDefinition in _plotDefinitions)
        {
            var plot = plotDefinition.ToPlot();
            world.RegisterPlot(plot);
        }
    }
}