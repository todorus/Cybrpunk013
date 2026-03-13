using Godot;
using SurveillanceStategodot.scripts.domain;
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

        Unsubscribe();

        _movement = movement;
        Subscribe();

        EmitSignalCharacterNameChanged(movement?.Character?.DisplayName);

        SyncImmediate();
    }

    public override void _ExitTree()
    {
        Unsubscribe();
        base._ExitTree();
    }

    private void Subscribe()
    {
        if (_movement?.Character == null)
            return;

        _movement.Character.Position.Changed += OnPositionChanged;
        _movement.Arrived += OnMovementArrived;
    }

    private void Unsubscribe()
    {
        if (_movement?.Character == null)
            return;

        _movement.Character.Position.Changed -= OnPositionChanged;
        _movement.Arrived -= OnMovementArrived;
    }

    private void OnPositionChanged(CharacterPosition position)
    {
        if (!IsInsideTree()) return;

        GlobalPosition = position.WorldPosition;

        if (position.Forward.LengthSquared() > 0.0001f)
            LookAt(position.WorldPosition + position.Forward, Vector3.Up, true);

        if (_movement != null)
            DrawRemainingPath(_movement, position);
    }

    private void OnMovementArrived(Movement movement)
    {
        ClearPath();
        QueueFree();
    }

    private void SyncImmediate()
    {
        if (_movement?.Character == null) return;
        if (!IsInsideTree()) return;

        var position = _movement.Character.Position;
        GlobalPosition = position.WorldPosition;

        if (position.Forward.LengthSquared() > 0.0001f)
            LookAt(position.WorldPosition + position.Forward, Vector3.Up, true);
    }

    private void DrawRemainingPath(Movement movement, CharacterPosition position)
    {
        if (_pathMesh == null || movement.Character?.IsOperator != true) return;

        _pathMesh.ClearSurfaces();

        var path = movement.Path;
        if (path == null || !path.IsValid || path.WorldPoints.Count < 2)
            return;

        _pathMesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip);

        // Start from the character's authoritative position.
        _pathMesh.SurfaceAddVertex(position.WorldPosition + Vector3.Up * _verticalOffset);

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