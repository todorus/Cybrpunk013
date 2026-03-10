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