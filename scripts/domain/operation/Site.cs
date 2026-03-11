using System;
using System.Collections.Generic;
using Godot;
using SurveillanceStategodot.scripts.domain.assignment;
using SurveillanceStategodot.scripts.domain.communication;
using SurveillanceStategodot.scripts.navigation.query;

namespace SurveillanceStategodot.scripts.domain.operation;

public sealed class Site
{
    private readonly List<Character> _occupants = new();
    private readonly List<SiteAsset> _assets = new();
    private readonly List<Operation> _activeOperations = new();
    private readonly List<Interceptor> _interceptors = new();

    public string Id { get; }
    public string Label { get; }
    public string BuildingId { get; }
    public SiteVisibility Visibility { get; set; } = SiteVisibility.Hidden;
    public string? BlockId { get; set; }
    public Vector3 GlobalPosition { get; set; }

    /// <summary>
    /// The closest point on the nav-graph to this site's world position.
    /// Precomputed once during bootstrapping.
    /// Used for pathfinding start/end anchors and as the world position for fixed VisionSources.
    /// Null until the nav graph has been stamped (see ScenarioBootstrapper).
    /// </summary>
    public DispatchNavEdgeAnchor? NavAnchor { get; set; }

    /// <summary>
    /// The world position agents navigate toward when heading to this site.
    /// Falls back to GlobalPosition when NavAnchor has not been stamped yet.
    /// </summary>
    public Vector3 EntryPosition => NavAnchor.HasValue ? NavAnchor.Value.Position : GlobalPosition;

    public Option[] AvailableOptions { get; set; } = [];

    public IReadOnlyList<Character> Occupants => _occupants;
    public IReadOnlyList<SiteAsset> Assets => _assets;
    public IReadOnlyList<Operation> ActiveOperations => _activeOperations;
    public IReadOnlyList<Interceptor> Interceptors => _interceptors;

    public event Action<Site, Operation>? ActiveOperationAdded;
    public event Action<Site, Operation>? ActiveOperationRemoved;
    
    public event Action<Site, Character>? OccupantAdded;
    public event Action<Site, Character>? OccupantRemoved;

    public Site(
        string id,
        string label,
        string buildingId,
        Vector3 globalPosition,
        Option[]? availableOptions = null)
    {
        Id = id;
        Label = label;
        BuildingId = buildingId;
        GlobalPosition = globalPosition;
        AvailableOptions = availableOptions ?? [];
    }

    public bool AddActiveOperation(Operation operation)
    {
        if (_activeOperations.Contains(operation))
            return false;

        _activeOperations.Add(operation);
        ActiveOperationAdded?.Invoke(this, operation);
        return true;
    }

    public bool RemoveActiveOperation(Operation operation)
    {
        if (!_activeOperations.Remove(operation))
            return false;

        ActiveOperationRemoved?.Invoke(this, operation);
        return true;
    }

    public bool AddOccupant(Character character)
    {
        if (_occupants.Contains(character))
            return false;

        _occupants.Add(character);
        OccupantAdded?.Invoke(this, character);
        return true;
    }

    public bool RemoveOccupant(Character character)
    {
        if(!_occupants.Remove(character))
            return false;
        
        OccupantRemoved?.Invoke(this, character);
        return true;
    }

    public bool AddAsset(SiteAsset asset)
    {
        if (_assets.Contains(asset))
            return false;

        _assets.Add(asset);
        return true;
    }

    public bool AddInterceptor(Interceptor interceptor)
    {
        if (_interceptors.Contains(interceptor))
            return false;

        _interceptors.Add(interceptor);
        return true;
    }
}