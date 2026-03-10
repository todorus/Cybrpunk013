using System.Collections.Generic;
using SurveillanceStategodot.scripts.authoring;
using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.plot;

public sealed class PlotSystem : ISimulationSystem
{
    private readonly IReadOnlyList<PlotResource> _plotDefinitions;

    private WorldState _world = null!;
    private bool _initialized;

    public void Initialize(WorldState world, SimulationEventBus eventBus)
    {
        _world = world;
    }

    public void Tick(double delta)
    {
        foreach (var plot in _world.Plots)
        {
            if (!plot.Initialized)
            {
                InitializePlot(plot);
            }
        }
    }
    
    private void InitializePlot(Plot plot)
    {
        foreach (var character in plot.Characters)
        {
            var firstScheduleEntry = character.Schedule?.Entries[0];
            if (firstScheduleEntry != null)
            {
                var initialSite = _world.GetSite(firstScheduleEntry.SiteId);
                character.CurrentSite = initialSite;
                initialSite.AddOccupant(character);
            }
        }
        
        plot.Initialized = true;
    }
}