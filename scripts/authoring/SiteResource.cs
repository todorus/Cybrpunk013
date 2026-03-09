using System;
using Godot;
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.authoring;

[GlobalClass]
public partial class SiteResource : Resource
{
    [Export]
    private string Id = Guid.NewGuid().ToString();
    
    [Export]
    private string Label = string.Empty;
    
    public Site ToSite(Vector3 globalPosition) => new(Id, Label, string.Empty, globalPosition);
}