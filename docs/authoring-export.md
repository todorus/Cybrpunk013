# Authoring Layer – Source Export

_Generated: 2026-03-14_

---

## `scripts/authoring/CharacterResource.cs`

```csharp
using System;
using Godot;
using SurveillanceStategodot.scripts.domain;
using SurveillanceStategodot.scripts.domain.movement;
using SurveillanceStategodot.scripts.domain.vision;

namespace SurveillanceStategodot.scripts.authoring;

[GlobalClass]
public partial class CharacterResource : Resource
{
    [Export] 
    private string _id = Guid.NewGuid().ToString();
    [Export] 
    private string _displayName = "";

    /// <summary>Stable identifier, matches the runtime Character.Id produced by ToCharacter().</summary>
    public string CharacterId => _id;

    /// <summary>Optional. Assign a ScheduleResource to give this character a baseline routine.</summary>
    [Export] 
    private ScheduleResource Schedule = null;

    /// <summary>
    /// World-units per second. Leave at 0 to use the simulation default.
    /// </summary>
    [Export]
    private float _movementSpeed = 3f;

    /// <summary>
    /// Vision radius in world-units. Leave at 0 to use the simulation default.
    /// </summary>
    [Export]
    private float _visionRange = 3f;

    /// <summary>
    /// Optional 3D scene used by PortraitStudio to render a portrait snapshot.
    /// The scene root should be positioned so it sits correctly at the origin of the SubjectAnchor.
    /// </summary>
    [Export]
    public PackedScene AvatarScene { get; private set; } = null;

    public Character ToCharacter()
    {
        var character = new Character(
            id: _id,
            displayName: _displayName)
        {
            Schedule = Schedule?.ToSchedule(),
            MovementSpeed = _movementSpeed,
            VisionRange = _visionRange
        };

        return character;
    }
}
```

## `scripts/authoring/OptionResource.cs`

```csharp
using System;
using Godot;
using SurveillanceStategodot.scripts.domain.assignment;
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.authoring;

[GlobalClass]
public partial class OptionResource : Resource
{
    [Export]
    private string _id = Guid.NewGuid().ToString();
    [Export]
    private string _label;
    [Export]
    private double _duration;
    [Export]
    private OperationVisionType _visionType = OperationVisionType.None;
    [Export]
    private ComplianceType _complianceType = ComplianceType.Compliant;
    
    public Option ToOption()
    {
        return new Option(
            id: _id,
            label: _label,
            duration: _duration,
            visionType: _visionType,
            complianceType: _complianceType);
    }
}
```

## `scripts/authoring/PlotResource.cs`

```csharp
using System;
using System.Collections.Generic;
using Godot;
using SurveillanceStategodot.scripts.domain.plot;

namespace SurveillanceStategodot.scripts.authoring;

[GlobalClass]
public partial class PlotResource : Resource
{
    [Export] private string _id = Guid.NewGuid().ToString();
    [Export] private string _label = "";
    [Export] private CharacterResource[] _characters = [];

    /// <summary>Exposes the authored character resources for indexing by ResourceRegistry.</summary>
    public IReadOnlyList<CharacterResource> Characters => _characters;
    
    public Plot ToPlot()
    {
        var plot = new Plot(
            id: _id,
            label: _label);
        
        foreach (var characterResource in _characters)
        {
            plot.Characters.Add(characterResource.ToCharacter());
        }

        return plot;
    }
}
```

## `scripts/authoring/ScenarioBootstrapper.cs`

```csharp
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


```

## `scripts/authoring/ScheduleEntryResource.cs`

```csharp
using Godot;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.domain.schedule;

namespace SurveillanceStategodot.scripts.authoring;

[GlobalClass]
public partial class ScheduleEntryResource : Resource
{
    /// <summary>Must match the Id of a SiteResource in the scene.</summary>
    [Export] 
    private SiteResource _site;

    [Export] 
    private string OperationLabel { get; set; } = "";
    
    [Export]
    private ComplianceType ComplianceType { get; set; }

    /// <summary>Dwell / operation duration in world-time seconds.</summary>
    [Export] public double Duration { get; set; } = 10.0;

    public ScheduleEntry ToScheduleEntry()
    {
        return new ScheduleEntry(
            siteId: _site.Id,
            operationLabel: OperationLabel,
            duration: Duration,
            complianceType: ComplianceType);
    }
}


```

## `scripts/authoring/ScheduleResource.cs`

```csharp
using System.Linq;
using Godot;
using SurveillanceStategodot.scripts.domain.schedule;

namespace SurveillanceStategodot.scripts.authoring;

[GlobalClass]
public partial class ScheduleResource : Resource
{
    [Export] public ScheduleEntryResource[] Entries { get; set; } = [];

    public Schedule ToSchedule()
    {
        var entries = Entries.Select(e => e.ToScheduleEntry()).ToArray();
        return new Schedule(entries);
    }
}


```

## `scripts/authoring/SiteResource.cs`

```csharp
using System;
using System.Linq;
using Godot;
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.authoring;

[GlobalClass]
public partial class SiteResource : Resource
{
    [Export]
    private string _id = Guid.NewGuid().ToString();
    public string Id => _id;
    
    [Export]
    private string _label = string.Empty;

    [Export] 
    private OptionResource[] _options = [];
    
    public Site ToSite(Vector3 globalPosition) => 
        new(
            _id, 
            _label, 
            string.Empty, 
            globalPosition, 
            _options.Select(resource => resource.ToOption()).ToArray()
        );
}
```

