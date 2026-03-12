using System.Linq;
using Godot;
using SurveillanceStategodot.scripts.authoring;
using SurveillanceStategodot.scripts.domain;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.interaction;

namespace SurveillanceStategodot.scripts.presentation.sites;

public partial class SiteNode : Node3D
{
    [Signal]
    public delegate void MaterialChangedEventHandler(Material newMaterial);
    
    [Export]
    public SiteResource SiteResource { get; private set; }
    
    [Export]
    private Material activeSiteMaterial;
    
    [Export]
    private Node3D uiAnchor = null!;
    
    public Vector3 UiWorldAnchor => uiAnchor.GlobalPosition;

    private SimulationController _simulationController;
    private SiteOverlayLayer? _overlayLayer;

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
            if (_site != null)
            {
                EmitSignalMaterialChanged(activeSiteMaterial);
                Register();
                _overlayLayer?.RefreshSite(this);
            }
        }
    }

    public override void _Ready()
    {
        base._Ready();
        if (SiteResource == null) return;
        
        Site = SiteResource.ToSite(GlobalPosition);

        if (IsActive)
            _overlayLayer?.ShowSite(this);
    }

    public void SetOverlayLayer(SiteOverlayLayer overlayLayer)
    {
        _overlayLayer = overlayLayer;

        if (IsActive)
            _overlayLayer?.ShowSite(this);
        else
            _overlayLayer?.HideSite(this);
    }

    private void Register()
    {
        if (Site == null || SimulationController == null) return;
        SimulationController.World.RegisterSite(Site);
    }

    public bool IsActive => SiteResource != null;
}