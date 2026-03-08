using Godot;
using SurveillanceStategodot.scripts.navigation.query;

namespace SurveillanceStategodot.scripts.presentation.movement;

/// <summary>
/// Renders a nav-path preview as a line strip using ImmediateMesh.
/// Call Show(path) to display a path and Hide() to clear it.
/// </summary>
public partial class PathPreviewVisual : MeshInstance3D
{
    
    [Export] private float _verticalOffset = 0.05f;

    [Export] public Color LineColor = new Color(0.2f, 1f, 0.2f);
    [Export] public float Width = 0.02f;

    private ImmediateMesh _mesh;
    private StandardMaterial3D _material;

    public override void _Ready()
    {
        _mesh = new ImmediateMesh();
        Mesh = _mesh;

        _material = new StandardMaterial3D();
        _material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        _material.AlbedoColor = LineColor;
        _material.VertexColorUseAsAlbedo = true;

        MaterialOverride = _material;
    }

    public void Show(DispatchNavPath path)
    {
        if (path == null || !path.IsValid || path.WorldPoints.Count < 2)
        {
            Hide();
            return;
        }

        _mesh.ClearSurfaces();
        _mesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip);

        foreach (var point in path.WorldPoints)
        {
            // offset slightly upward so the line sits above the ground
            _mesh.SurfaceAddVertex(point + Vector3.Up * _verticalOffset);
        }

        _mesh.SurfaceEnd();

        Visible = true;
    }

    public new void Hide()
    {
        _mesh?.ClearSurfaces();
        Visible = false;
    }

    /// <summary>
    /// Use this as the signal receiver for CityscapeHoverHandler.PathPreviewChanged.
    /// </summary>
    public void OnPathPreviewChanged(DispatchNavPath path)
    {
        if (path == null)
            Hide();
        else
            Show(path);
    }
}
