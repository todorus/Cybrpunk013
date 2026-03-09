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
    public delegate void LabelChangedEventHandler(string newLabel);
    
    [Signal]
    public delegate void MaterialChangedEventHandler(Material newMaterial);
    
    [Signal]
    public delegate void OperationsListChangedEventHandler(string list);
    [Signal]
    public delegate void OccupantsListChangedEventHandler(string list);
    
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
            Unsubscribe();
            _site = value;
            Subscribe();
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

    public override void _ExitTree()
    {
        base._ExitTree();
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (Site == null) return;
        Site.ActiveOperationAdded += OnOperationsChanged;
        Site.ActiveOperationRemoved += OnOperationsChanged;
        Site.OccupantAdded += OnOccupantsChanged;
        Site.OccupantRemoved += OnOccupantsChanged;
    }
    
    private void Unsubscribe()
    {
        if (Site == null) return;
        Site.ActiveOperationAdded -= OnOperationsChanged;
        Site.ActiveOperationRemoved -= OnOperationsChanged;
        Site.OccupantAdded -= OnOccupantsChanged;
        Site.OccupantRemoved -= OnOccupantsChanged;
    }
    
    private void OnOperationsChanged(Site site, Operation operation)
    {
        EmitSignalOperationsListChanged(string.Join(", ", site.ActiveOperations.Select(op => op.Label)));
    }

    private void OnOccupantsChanged(Site site, Character character)
    {
        EmitSignalOccupantsListChanged(string.Join(", ", site.Occupants.Select(op => op.DisplayName)));
    }

    private void Register()
    {
        if (Site == null || SimulationController == null) return;
        SimulationController.World.RegisterSite(Site);
    }

    public bool IsActive => siteResource != null;
}