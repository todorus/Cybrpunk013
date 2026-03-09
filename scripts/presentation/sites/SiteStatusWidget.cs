using System.Linq;
using Godot;
using SurveillanceStategodot.scripts.domain;
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.presentation.sites;

public partial class SiteStatusWidget : Control
{
    [Signal]
    public delegate void LabelChangedEventHandler(string newLabel);
    
    [Signal]
    public delegate void OperationsListChangedEventHandler(string list);
    [Signal]
    public delegate void OccupantsListChangedEventHandler(string list);

    public SiteNode? SiteNode { get; private set; }
    
    private Site Site => SiteNode?.Site;

    public void Bind(SiteNode siteNode)
    {
        Unsubscribe();
        SiteNode = siteNode;
        Subscribe();
        Refresh();
    }

    public void Refresh()
    {
        EmitSignalLabelChanged(Site?.Label);
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
}