using Godot;
using SurveillanceStategodot.scripts.domain;
using SurveillanceStategodot.scripts.domain.movement;

namespace SurveillanceStategodot.scripts.presentation.movement;

/// <summary>
/// Map-level visual for a character (operator or NPC). Tracks CharacterPosition,
/// so it survives across movements and remains visible when the character is
/// stationary (e.g. holding position, staking out, or idle between schedule entries).
/// Draws the remaining path when a movement is active; clears it otherwise.
/// </summary>
public partial class CharacterMapVisual : Node3D
{
    [Signal]
    public delegate void CharacterNameChangedEventHandler(string newName);

    [Export] private float _verticalOffset = 0.05f;
    [Export] public Color LineColor = new Color(0.2f, 1f, 0.2f);

    private Character? _character;
    private ImmediateMesh _pathMesh = null!;
    private MeshInstance3D _pathMeshInstance = null!;

    public Character? TrackedCharacter => _character;

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
        _pathMeshInstance.TopLevel = true;
        AddChild(_pathMeshInstance);
    }

    public void SetCharacter(Character character)
    {
        if (_character == character)
            return;

        Unsubscribe();

        _character = character;
        Subscribe();

        EmitSignalCharacterNameChanged(character?.DisplayName);

        SyncImmediate();
    }

    public override void _ExitTree()
    {
        Unsubscribe();
        base._ExitTree();
    }

    private void Subscribe()
    {
        if (_character == null) return;
        _character.Position.Changed += OnPositionChanged;
    }

    private void Unsubscribe()
    {
        if (_character == null) return;
        _character.Position.Changed -= OnPositionChanged;
    }

    private void OnPositionChanged(CharacterPosition position)
    {
        if (!IsInsideTree()) return;

        GlobalPosition = position.WorldPosition;

        if (position.Forward.LengthSquared() > 0.0001f)
            LookAt(position.WorldPosition + position.Forward, Vector3.Up, true);

        DrawRemainingPath();
    }

    private void SyncImmediate()
    {
        if (_character == null || !IsInsideTree()) return;

        var position = _character.Position;
        GlobalPosition = position.WorldPosition;

        if (position.Forward.LengthSquared() > 0.0001f)
            LookAt(position.WorldPosition + position.Forward, Vector3.Up, true);

        DrawRemainingPath();
    }

    private void DrawRemainingPath()
    {
        if (_pathMesh == null || _character == null) return;

        _pathMesh.ClearSurfaces();

        // Only draw path for operators.
        if (!_character.IsOperator) return;

        var movement = _character.CurrentMovement;
        if (movement == null) return;

        var path = movement.Path;
        if (path == null || !path.IsValid || path.WorldPoints.Count < 2) return;

        _pathMesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip);
        _pathMesh.SurfaceAddVertex(_character.Position.WorldPosition + Vector3.Up * _verticalOffset);

        for (int i = movement.SegmentIndex + 1; i < path.WorldPoints.Count; i++)
        {
            _pathMesh.SurfaceAddVertex(path.WorldPoints[i] + Vector3.Up * _verticalOffset);
        }

        _pathMesh.SurfaceEnd();
    }
}