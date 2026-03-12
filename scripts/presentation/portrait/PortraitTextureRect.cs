using Godot;
using SurveillanceStategodot.scripts.authoring;

namespace SurveillanceStategodot.scripts.presentation.portrait;

/// <summary>
/// A TextureRect that requests a portrait from PortraitCache and displays it.
///
/// Usage:
///   1. Place this node in any UI scene.
///   2. Wire the exported PortraitCache reference.
///   3. Call SetCharacter(characterResource) at runtime.
///
/// The control is intentionally thin — it delegates all rendering/caching
/// to PortraitCache and PortraitStudio.
/// </summary>
public partial class PortraitTextureRect : TextureRect
{
    [Export] private PortraitCache _cache = null!;

    [ExportGroup("Optional Cache Keys")]
    /// <summary>
    /// Optional appearance key. If the character's visible appearance can vary,
    /// set this before calling SetCharacter so the cache key is unique per look.
    /// </summary>
    [Export] public string AppearanceKey { get; set; } = "";

    /// <summary>
    /// Optional preset key for future camera/lighting presets.
    /// Leave empty for the default studio setup.
    /// </summary>
    [Export] public string PresetKey { get; set; } = "";

    private CharacterResource? _characterResource;

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Connect OperatorDisplay.OperatorChanged to this method in the editor.
    /// Godot passes custom Resource subclasses as plain Resource across signal
    /// boundaries, so this method accepts Resource and casts it safely.
    /// async void methods cannot be connected via the editor signal panel —
    /// this non-async wrapper exists for that reason.
    /// </summary>
    public void SetCharacterFromSignal(Resource resource)
    {
        if (resource is CharacterResource characterResource)
            SetCharacter(characterResource);
        else
            GD.PushWarning($"[PortraitTextureRect] SetCharacterFromSignal received unexpected type: {resource?.GetType()}");
    }
    
    /// <summary>
    /// Requests the portrait for <paramref name="characterResource"/> and applies
    /// it as this control's texture once available.
    /// Safe to call repeatedly; previous requests are implicitly superseded.
    /// </summary>
    public async void SetCharacter(CharacterResource characterResource)
    {
        _characterResource = characterResource;
        Texture = null; // Clear while loading.

        if (_cache == null)
        {
            GD.PushWarning("[PortraitTextureRect] PortraitCache reference is not set.");
            return;
        }

        var texture = await _cache.GetOrRenderAsync(
            characterResource.CharacterId,
            characterResource.AvatarScene,
            AppearanceKey,
            PresetKey);

        // Guard: node may have been freed or character changed while awaiting.
        if (!IsInstanceValid(this) || _characterResource != characterResource)
            return;

        Texture = texture;
    }
}


