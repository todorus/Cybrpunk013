using System.Collections.Generic;
using Godot;
using SurveillanceStategodot.scripts.authoring;
using SurveillanceStategodot.scripts.presentation.portrait;
using SurveillanceStategodot.scripts.util;

namespace SurveillanceStategodot.scripts.presentation.sites;

public partial class SiteOverlayLayer : Control
{
    [Signal]
    public delegate void CharacterClickedEventHandler(CharacterResource characterResource);
    
    [Export]
    private ResourceRegistry _resourceRegistry;
    
    [Export]
    private PortraitCache _portraitCache;
    
    private sealed class Binding
    {
        public SiteNode Site { get; }
        public SiteStatusWidget Widget { get; }

        public Binding(SiteNode site, SiteStatusWidget widget)
        {
            Site = site;
            Widget = widget;
        }
    }

    [Export]
    private PackedScene? _widgetScene;

    [Export]
    private Camera3D? _camera;

    private readonly Dictionary<SiteNode, Binding> _activeBindings = new();

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        BootstrapSites();
    }

    public override void _Process(double delta)
    {
        UpdateWidgetPositions();
    }

    private void BootstrapSites()
    {
        var sites = GetTree().Root.FindAllChildrenOfType<SiteNode>();

        foreach (var site in sites)
            site.SetOverlayLayer(this);
    }

    public void ShowSite(SiteNode site)
    {
        if (!IsInstanceValid(site))
            return;

        if (_activeBindings.ContainsKey(site))
        {
            RefreshSite(site);
            return;
        }

        var widget = CreateWidget(site);
        if (widget == null)
            return;

        AddChild(widget);

        var binding = new Binding(site, widget);
        _activeBindings.Add(site, binding);

        widget.Bind(site);
    }

    public void HideSite(SiteNode site)
    {
        if (!_activeBindings.TryGetValue(site, out var binding))
            return;

        if (IsInstanceValid(binding.Widget))
            binding.Widget.QueueFree();

        _activeBindings.Remove(site);
    }

    public void RefreshSite(SiteNode site)
    {
        if (!_activeBindings.TryGetValue(site, out var binding))
            return;

        if (!IsInstanceValid(binding.Widget))
        {
            _activeBindings.Remove(site);
            return;
        }

        binding.Widget.Refresh();
    }

    private SiteStatusWidget? CreateWidget(SiteNode site)
    {
        if (_widgetScene == null)
        {
            GD.PushError($"{nameof(SiteOverlayLayer)}: Widget scene is not assigned.");
            return null;
        }

        var widget = _widgetScene.Instantiate<SiteStatusWidget>();
        widget.ResourceRegistry = _resourceRegistry;
        widget.PortraitCache = _portraitCache;
        widget.OccupantClicked += EmitSignalCharacterClicked;
        return widget;
    }

    private void UpdateWidgetPositions()
    {
        var camera = ResolveCamera();
        if (camera == null)
            return;

        Rect2 viewportRect = GetViewportRect();
        var deadSites = new List<SiteNode>();

        foreach (var pair in _activeBindings)
        {
            var site = pair.Key;
            var binding = pair.Value;

            if (!IsInstanceValid(site) || !IsInstanceValid(binding.Widget))
            {
                deadSites.Add(site);
                continue;
            }

            Vector3 worldPos = site.UiWorldAnchor;

            if (IsBehindCamera(camera, worldPos))
            {
                binding.Widget.Visible = false;
                continue;
            }

            Vector2 screenPos = camera.UnprojectPosition(worldPos);

            if (!viewportRect.HasPoint(screenPos))
            {
                binding.Widget.Visible = false;
                continue;
            }

            binding.Widget.Visible = true;

            Vector2 size = binding.Widget.Size;
            binding.Widget.Position = screenPos + new Vector2(
                -size.X * 0.5f,
                -size.Y
            );
        }

        foreach (var deadSite in deadSites)
            _activeBindings.Remove(deadSite);
    }

    private Camera3D? ResolveCamera()
    {
        if (IsInstanceValid(_camera))
            return _camera;

        return GetViewport().GetCamera3D();
    }

    private static bool IsBehindCamera(Camera3D camera, Vector3 worldPos)
    {
        Vector3 toPoint = worldPos - camera.GlobalPosition;
        return camera.GlobalBasis.Z.Dot(toPoint) > 0f;
    }
}