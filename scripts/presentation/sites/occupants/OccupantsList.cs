using System.Threading;
using Godot;
using SurveillanceStategodot.scripts.authoring;
using SurveillanceStategodot.scripts.presentation.portrait;
using SurveillanceStategodot.scripts.presentation.primitives;
using SurveillanceStategodot.scripts.util;

namespace SurveillanceStategodot.scripts.presentation.sites.occupants;

public partial class OccupantsList : Container
{
    [Export]
    private PackedScene _occupantScene;
    
    [Signal]
    public delegate void OccupantClickedEventHandler(CharacterResource characterResource);

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

    private CancellationTokenSource _refreshCts;

    public override void _Ready()
    {
        base._Ready();
        Refresh(null);
    }

    public async void Refresh(SiteNode siteNode)
    {
        // Cancel any in-flight refresh and start a fresh one.
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _refreshCts = new CancellationTokenSource();
        var token = _refreshCts.Token;

        // Clear immediately so stale widgets are gone before awaiting.
        Cleanup();

        if (siteNode?.Site == null) return;

        foreach (var occupant in siteNode.Site.Occupants)
        {
            if (token.IsCancellationRequested) return;
            if (!_resourceRegistry.TryGetCharacter(occupant.Id, out var occupantResource)) continue;

            var avatar = await _portraitCache.GetOrRenderAsync(occupantResource);

            // Check again after the await — a newer Refresh may have started.
            if (token.IsCancellationRequested) return;

            var occupantWidget = _occupantScene.Instantiate<TextureClickable>();
            occupantWidget.Texture = avatar;
            occupantWidget.Resource = occupantResource;
            occupantWidget.Clicked += OnOccupantClicked;
            AddChild(occupantWidget);
        }
    }

    public override void _ExitTree()
    {
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
    }

    private void Cleanup()
    {
        this.FindAllChildrenOfType<TextureClickable>()
            .ForEach(occupantDisplay => occupantDisplay.Clicked -= OnOccupantClicked);
        this.ClearChildren();
    }

    private void OnOccupantClicked(Resource resource)
    {
        if (resource is not CharacterResource characterResource) return;

        // EmitSignalOccupantClicked(characterResource);
    }
}