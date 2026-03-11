using Godot;
using SurveillanceStategodot.scripts.domain.movement;

namespace SurveillanceStategodot.scripts.presentation.movement;

public partial class MovementVisual : Node3D
{
    [Signal]
    public delegate void CharacterNameChangedEventHandler(string newName);

    [Export] private float _verticalOffset = 0.05f;
    [Export] public Color LineColor = new Color(0.2f, 1f, 0.2f);

    private Movement? _movement;
    private ImmediateMesh _pathMesh = null!;
    private MeshInstance3D _pathMeshInstance = null!;

    public override void _Ready()
    {
        _pathMesh = new ImmediateMesh();

        var material = new StandardMaterial3D();
        material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        material.AlbedoColor = LineColor;
        material.VertexColorUseAsAlbedo = true;

        _pathMeshInstance = new MeshInstance3D();
        _pathMeshInstance.Mesh = _pathMesh;
        _pathMeshInstance.MaterialOverride = material;
        // Use global (non-local) space so the mesh instance position doesn't affect the line
        _pathMeshInstance.TopLevel = true;
        AddChild(_pathMeshInstance);
    }

    public void SetMovement(Movement movement)
    {
        if (_movement == movement)
            return;

        UnsubscribeFromMovement();

        _movement = movement;
        SubscribeToMovement();
        
        EmitSignalCharacterNameChanged(movement?.Character?.DisplayName);

        SyncImmediate();
    }
    

    public override void _ExitTree()
    {
        UnsubscribeFromMovement();
        base._ExitTree();
    }

    private void SubscribeToMovement()
    {
        if (_movement == null)
            return;

        _movement.PositionChanged += OnMovementPositionChanged;
        _movement.Arrived += OnMovementArrived;
    }

    private void UnsubscribeFromMovement()
    {
        if (_movement == null)
            return;

        _movement.PositionChanged -= OnMovementPositionChanged;
        _movement.Arrived -= OnMovementArrived;
    }

    private void OnMovementPositionChanged(Movement movement)
    {
        GlobalPosition = movement.CurrentWorldPosition;

        if (movement.CurrentForward.LengthSquared() > 0.0001f)
            LookAt(movement.CurrentWorldPosition + movement.CurrentForward, Vector3.Up, true);

        DrawRemainingPath(movement);
    }

    private void OnMovementArrived(Movement movement)
    {
        ClearPath();
        QueueFree();
    }

    private void SyncImmediate()
    {
        if (_movement == null)
            return;

        GlobalPosition = _movement.CurrentWorldPosition;

        if (_movement.CurrentForward.LengthSquared() > 0.0001f)
            LookAt(_movement.CurrentWorldPosition + _movement.CurrentForward, Vector3.Up, true);

        DrawRemainingPath(_movement);
    }

    private void DrawRemainingPath(Movement movement)
    {
        if (_pathMesh == null)
            return;

        _pathMesh.ClearSurfaces();

        var path = movement.Path;
        if (path == null || !path.IsValid || path.WorldPoints.Count < 2)
            return;

        _pathMesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip);

        // Start from the current world position, then follow remaining waypoints
        _pathMesh.SurfaceAddVertex(movement.CurrentWorldPosition + Vector3.Up * _verticalOffset);

        for (int i = movement.SegmentIndex + 1; i < path.WorldPoints.Count; i++)
        {
            _pathMesh.SurfaceAddVertex(path.WorldPoints[i] + Vector3.Up * _verticalOffset);
        }

        _pathMesh.SurfaceEnd();
    }

    private void ClearPath()
    {
        _pathMesh?.ClearSurfaces();
    }
}