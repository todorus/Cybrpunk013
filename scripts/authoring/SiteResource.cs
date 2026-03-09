using System;
using Godot;
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.authoring;

[GlobalClass]
public partial class SiteResource : Resource
{
    [Export]
    private string _id = Guid.NewGuid().ToString();
    
    [Export]
    private string _label = string.Empty;

    [Export] 
    private OperationResource _operations;
    
    public Site ToSite(Vector3 globalPosition) => new(_id, _label, string.Empty, globalPosition);
}