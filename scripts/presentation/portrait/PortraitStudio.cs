using Godot;
using System.Threading.Tasks;

namespace SurveillanceStategodot.scripts.presentation.portrait;

/// <summary>
/// Controls a reusable offscreen portrait-rendering setup.
///
/// Expected scene structure (AutomatedPortrait.tscn):
///   Node3D  (root, this script)
///   └─ SubViewport
///      └─ PortraitStudio  (Node3D)
///         ├─ Camera3D
///         ├─ Lights ...
///         └─ SubjectAnchor  (Node3D)
///
/// The studio owns visual consistency (lighting, camera, background).
/// The character scene owns only the avatar itself.
/// </summary>
public partial class PortraitStudio : Node
{
    [Export] private SubViewport _viewport = null!;
    [Export] private Node3D _subjectAnchor = null!;

    private Node3D? _currentSubject;

    public override void _Ready()
    {
        // Keep the viewport paused between renders — we render on demand.
        _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
    }

    /// <summary>Removes any previously placed subject from the SubjectAnchor.</summary>
    public void ClearSubject()
    {
        if (_currentSubject == null)
            return;

        _subjectAnchor.RemoveChild(_currentSubject);
        _currentSubject.QueueFree();
        _currentSubject = null;
    }

    /// <summary>
    /// Instantiates <paramref name="avatarScene"/> and places it under SubjectAnchor
    /// at local origin / identity transform.
    /// Clears any previous subject first.
    /// </summary>
    public void SetSubject(PackedScene avatarScene)
    {
        ClearSubject();

        var instance = avatarScene.Instantiate<Node3D>();
        instance.Transform = Transform3D.Identity;
        _subjectAnchor.AddChild(instance);
        _currentSubject = instance;
    }

    /// <summary>
    /// Triggers a one-shot render and returns a snapshot <see cref="ImageTexture"/>.
    /// Waits two frames to ensure the renderer has processed the scene before reading.
    /// Returns null if the viewport or image is invalid.
    /// </summary>
    public async Task<ImageTexture?> RenderSnapshotAsync()
    {
        // Request a single render.
        _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;

        // Wait two frames: one for the render to be submitted, one for it to complete.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        // Restore disabled mode.
        _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;

        var image = _viewport.GetTexture().GetImage();
        if (image == null || image.IsEmpty())
        {
            GD.PushWarning("[PortraitStudio] Snapshot image was empty.");
            return null;
        }

        var texture = ImageTexture.CreateFromImage(image);
        return texture;
    }

    public override void _ExitTree()
    {
        ClearSubject();
    }
}

