using System.Collections.Generic;
using System.Linq;
using Godot;
using SurveillanceStategodot.scripts.domain.assignment;
using SurveillanceStategodot.scripts.domain.communication;

namespace SurveillanceStategodot.scripts.domain.operation;

public sealed class Site
{
    public string Id { get; }
    public string Label { get;  }
    public string BuildingId { get; }
    public SiteVisibility Visibility { get; set; } = SiteVisibility.Hidden;
    public string? BlockId { get; set; }
    public Vector3 GlobalPosition { get; set; }
    public Option[] AvailableOptions { get; set; } = [];

    public List<Character> Occupants => ActiveOperations
        .SelectMany(operation => operation.Participants)
        .ToList();
    public List<SiteAsset> Assets { get; } = new();
    public List<Operation> ActiveOperations { get; } = new();
    public List<Interceptor> Interceptors { get; } = new();

    public Site(string id, string label, string buildingId, Vector3 globalPosition, Option[] availableOptions = null)
    {
        Id = id;
        Label = label;
        BuildingId = buildingId;
        GlobalPosition = globalPosition;
        AvailableOptions = availableOptions ?? [];
    }
}