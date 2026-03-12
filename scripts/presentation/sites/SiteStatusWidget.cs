using System.Linq;
using Godot;
using SurveillanceStategodot.scripts.domain;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.presentation.portrait;

namespace SurveillanceStategodot.scripts.presentation.sites;

public partial class SiteStatusWidget : Control
{
    [Signal]
    public delegate void LabelChangedEventHandler(string newLabel);
    
    [Signal]
    public delegate void OperationsListChangedEventHandler(string list);
    [Signal]
    public delegate void OccupantsListChangedEventHandler(SiteNode siteNode);
    
    [Signal]
    public delegate void ResourceRegistryChangedEventHandler(ResourceRegistry registry);
    [Signal]
    public delegate void PortraitCacheChangedEventHandler(PortraitCache cache);

    public SiteNode? SiteNode { get; private set; }
    
    private Site Site => SiteNode?.Site;

    private ResourceRegistry _resourceRegistry;
    public ResourceRegistry ResourceRegistry
    {
        get => _resourceRegistry;
        set
        {
            _resourceRegistry = value;
            EmitSignalResourceRegistryChanged(_resourceRegistry);
        }
    }
    
    private PortraitCache _portraitCache;
    public PortraitCache PortraitCache 
    {
        get => _portraitCache;
        set
        {
            _portraitCache = value;
            EmitSignalPortraitCacheChanged(_portraitCache);
        }
    }

    public override void _Ready()
    {
        base._Ready();
        EmitSignalResourceRegistryChanged(_resourceRegistry);
        EmitSignalPortraitCacheChanged(_portraitCache);
    }

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
        EmitSignalOccupantsListChanged(SiteNode);
    }
}