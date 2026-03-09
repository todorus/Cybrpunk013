using Godot;
using SurveillanceStategodot.scripts.authoring;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.interaction;

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

    private SimulationController _simulationController;

    public SimulationController SimulationController
    {
        get => _simulationController;
        set
        {
            _simulationController = value;
            Register();
        }
    }

    private Site _site;
    public Site Site
    {
        get => _site;
        private set
        {
            _site = value;
            EmitSignalLabelChanged(_site?.Label);
            if (_site != null)
            {
                EmitSignalMaterialChanged(activeSiteMaterial);
                Register();
            }
        }
    }

    public override void _Ready()
    {
        base._Ready();
        if (siteResource == null) return;
        
        Site = siteResource.ToSite(GlobalPosition);
    }
    
    private void Register()
    {
        if (Site == null || SimulationController == null) return;
        SimulationController.World.RegisterSite(Site);
    }

    public bool IsActive => siteResource != null;
}