using Godot;
using SurveillanceStategodot.scripts.authoring;

namespace SurveillanceStategodot.scripts.presentation.sites;

public partial class SiteNode : Node3D
{
    [Signal]
    public delegate void LabelChangedEventHandler(string newLabel);
    
    [Signal]
    public delegate void MaterialChangedEventHandler(Material newMaterial);
    
    [Export]
    private SiteResource siteResource;
    
    [Export]
    private Material activeSiteMaterial;

    public override void _Ready()
    {
        base._Ready();
        if (siteResource == null) return;
        
        var site = siteResource.ToSite();
        EmitSignalLabelChanged(site.Label);
        EmitSignalMaterialChanged(activeSiteMaterial);
        
        GD.Print($"SiteNode ready with site: {site.Id}, label: {site.Label}");
    }
}