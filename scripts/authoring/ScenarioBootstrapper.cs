using Godot;
using SurveillanceStategodot.scripts.domain;
using SurveillanceStategodot.scripts.domain.plot;
using SurveillanceStategodot.scripts.interaction;
using SurveillanceStategodot.scripts.navigation.authoring;
using SurveillanceStategodot.scripts.navigation.query;
using SurveillanceStategodot.scripts.presentation.sites;
using SurveillanceStategodot.scripts.util;

namespace SurveillanceStategodot.scripts.authoring;

public partial class ScenarioBootstrapper : Node
{
    [Export]
    private PlotResource[] _plotDefinitions = [];

    [Export]
    private DispatchNav _dispatchNav = null!;

    public void Init(SimulationController simulationController)
    {
        InitializeSites(simulationController);
        StampSiteNavAnchors(simulationController);
        InitializePlots(simulationController);
    }

    private void InitializeSites(SimulationController simulationController)
    {
        GetTree().Root.FindAllChildrenOfType<SiteNode>()
            .ForEach(siteNode => siteNode.SimulationController = simulationController);
    }

    /// <summary>
    /// Precomputes the closest nav-graph anchor for every registered site.
    /// Must run after sites are registered and before plots (which spawn characters) are initialized.
    /// </summary>
    private void StampSiteNavAnchors(SimulationController simulationController)
    {
        if (_dispatchNav?.Graph == null)
        {
            GD.PushWarning("[ScenarioBootstrapper] DispatchNav not set — site NavAnchors will not be precomputed.");
            return;
        }

        foreach (var site in simulationController.World.Sites)
        {
            if (DispatchNavSpawnQueries.TryGetSpawnPoint(_dispatchNav.Graph, site.GlobalPosition, out var anchor))
            {
                site.NavAnchor = anchor;
            }
            else
            {
                GD.PushWarning($"[ScenarioBootstrapper] Could not compute NavAnchor for site '{site.Id}' ({site.Label}).");
            }
        }
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