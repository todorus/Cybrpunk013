using Godot;
using SurveillanceStategodot.scripts.interaction;
using SurveillanceStategodot.scripts.navigation.authoring;
using SurveillanceStategodot.scripts.navigation.query;
using SurveillanceStategodot.scripts.presentation;
using SurveillanceStategodot.scripts.presentation.sites;
using SurveillanceStategodot.scripts.util;

namespace SurveillanceStategodot.scripts.authoring;

public partial class ScenarioBootstrapper : Node
{
    [Export]
    private CharacterResource[] _operatorDefinitions = [];
    
    [Export]
    private PlotResource[] _plotDefinitions = [];

    [Export]
    private DispatchNav _dispatchNav = null!;

    /// <summary>
    /// Optional. When assigned, ScenarioBootstrapper will register all
    /// CharacterResources and SiteResources it knows about into this registry
    /// so presentation nodes can look them up by domain ID.
    /// </summary>
    [Export]
    private ResourceRegistry _resourceRegistry = null!;

    public void Init(SimulationController simulationController)
    {
        PopulateResourceRegistry();
        InitializeSites(simulationController);
        StampSiteNavAnchors(simulationController);
        InitializeOperators(simulationController);
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

    private void InitializeOperators(SimulationController simulationController)
    {
        var world = simulationController.World;
        foreach (var operatorDefinition in _operatorDefinitions)
        {
            var character = operatorDefinition.ToCharacter();
            world.RegisterOperator(character);
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

    /// <summary>
    /// Registers all authored resources the bootstrapper knows about into the
    /// ResourceRegistry so presentation nodes can look them up by domain ID.
    /// No-op when _resourceRegistry is not assigned.
    /// </summary>
    private void PopulateResourceRegistry()
    {
        if (_resourceRegistry == null) return;

        foreach (var plotDefinition in _plotDefinitions)
        {
            foreach (var characterResource in plotDefinition.Characters)
            {
                _resourceRegistry.RegisterCharacter(characterResource);
            }
        }

        foreach (var siteNode in GetTree().Root.FindAllChildrenOfType<SiteNode>())
        {
            if (siteNode.SiteResource != null)
                _resourceRegistry.RegisterSite(siteNode.SiteResource);
        }

        foreach (var characterResource in _operatorDefinitions)
        {
            _resourceRegistry.RegisterCharacter(characterResource);
        }
    }
}

