using Godot;
using SurveillanceStategodot.scripts.navigation.authoring;
using SurveillanceStategodot.scripts.navigation.query;
using SurveillanceStategodot.scripts.presentation.sites;

namespace SurveillanceStategodot.scripts.interaction;

/// <summary>
/// Listens for mouse-move events and shows a path preview when the cursor
/// hovers over an active SiteNode, using the same nav-graph logic as
/// CityscapeClickHandler.
/// </summary>
public partial class CityscapeHoverHandler : Node
{
    [Export] private DispatchNav _dispatchNav = null!;
    [Export] private Node3D _spawnWorldPosition = null!;

    /// <summary>
    /// Emitted when the hovered path changes. Path is null when no valid path exists.
    /// </summary>
    [Signal]
    public delegate void PathPreviewChangedEventHandler(DispatchNavPath path);

    /// <summary>
    /// Connect MouseSignals.MoveEvent to this method.
    /// </summary>
    public void HandleMove(GodotObject obj, Vector3 position, Vector2 delta)
    {
        if (obj is SiteNode siteNode && siteNode.IsActive)
        {
            TryShowPathPreview(siteNode);
        }
        else
        {
            EmitSignalPathPreviewChanged(null);
        }
    }

    private void TryShowPathPreview(SiteNode siteNode)
    {
        if (!DispatchNavSpawnQueries.TryGetSpawnPoint(
                _dispatchNav.Graph,
                _spawnWorldPosition.GlobalPosition,
                out var spawnAnchor))
        {
            EmitSignalPathPreviewChanged(null);
            return;
        }

        var endPoint = DispatchNavQueries.GetClosestPointOnGraph(
            _dispatchNav.Graph,
            siteNode.GlobalPosition);

        var path = DispatchNavPathfinder.FindPath(_dispatchNav.Graph, spawnAnchor, endPoint);

        EmitSignalPathPreviewChanged(path);
    }
}
