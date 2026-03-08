using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain.communication;

namespace SurveillanceStategodot.scripts.domain.operation;

public sealed class Site
{
    public string Id { get; }
    public string BuildingId { get; }
    public SiteVisibility Visibility { get; set; } = SiteVisibility.Hidden;
    public string? BlockId { get; set; }

    public List<Character> Occupants { get; } = new();
    public List<SiteAsset> Assets { get; } = new();
    public List<Operation> ActiveOperations { get; } = new();
    public List<Interceptor> Interceptors { get; } = new();

    public Site(string id, string buildingId)
    {
        Id = id;
        BuildingId = buildingId;
    }
}