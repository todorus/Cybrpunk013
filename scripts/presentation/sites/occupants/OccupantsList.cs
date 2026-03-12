using Godot;
using SurveillanceStategodot.scripts.presentation.portrait;
using SurveillanceStategodot.scripts.util;

namespace SurveillanceStategodot.scripts.presentation.sites.occupants;

public partial class OccupantsList : Container
{
    [Export]
    private PackedScene _occupantScene;

    private PortraitCache _portraitCache;
    private void SetPortraitCache(PortraitCache portraitCache)
    {
        _portraitCache = portraitCache;
    }
    
    private ResourceRegistry _resourceRegistry;
    private void SetResourceRegistry(ResourceRegistry resourceRegistry)
    {
        _resourceRegistry = resourceRegistry;
    }

    public override void _Ready()
    {
        base._Ready();
        Refresh(null);
    }

    public void Refresh(SiteNode siteNode)
    {
        this.ClearChildren();
        if (siteNode.Site == null) return;
        foreach (var occupant in siteNode.Site.Occupants)
        {
            if(!_resourceRegistry.TryGetCharacter(occupant.Id, out var occupantResource)) continue;
            
            var occupantWidget = _occupantScene.Instantiate<PortraitTextureRect>();
            occupantWidget.Cache = _portraitCache;
            occupantWidget.SetCharacter(occupantResource);
            AddChild(occupantWidget);
        }
    }
}