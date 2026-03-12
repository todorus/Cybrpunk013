using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace SurveillanceStategodot.scripts.presentation.portrait;

/// <summary>
/// Singleton-style Node that stores rendered portrait textures and provides
/// a "get-or-render" flow.
///
/// Place one instance of this node (portrait_cache.tscn) in the scene tree,
/// with a reference to the shared PortraitStudio.
///
/// Cache key = "{characterId}::{appearanceKey}::{presetKey}"
/// Pass empty string for appearanceKey / presetKey when not used.
/// </summary>
public partial class PortraitCache : Node
{
    [Export] private PortraitStudio _studio = null!;

    private readonly Dictionary<string, ImageTexture> _cache = new();

    // Simple lock flag: prevents concurrent renders through the single studio.
    private bool _rendering = false;

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a cached portrait texture if available, otherwise renders one
    /// through the studio, caches it, and returns it.
    ///
    /// <paramref name="avatarScene"/> must be set; if null this returns null immediately.
    /// <paramref name="appearanceKey"/> should encode visible appearance (e.g. outfit/colour hash).
    /// <paramref name="presetKey"/> can select a future camera/lighting preset; pass "" for default.
    /// </summary>
    public async Task<ImageTexture?> GetOrRenderAsync(
        string characterId,
        PackedScene? avatarScene,
        string appearanceKey = "",
        string presetKey = "")
    {
        if (avatarScene == null)
            return null;

        var key = BuildKey(characterId, appearanceKey, presetKey);

        if (_cache.TryGetValue(key, out var cached))
            return cached;

        // Wait if another render is in flight.
        while (_rendering)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        // Re-check after waiting — another caller may have rendered the same key.
        if (_cache.TryGetValue(key, out cached))
            return cached;

        _rendering = true;
        try
        {
            _studio.SetSubject(avatarScene);
            var texture = await _studio.RenderSnapshotAsync();
            _studio.ClearSubject();

            if (texture != null)
                _cache[key] = texture;

            return texture;
        }
        finally
        {
            _rendering = false;
        }
    }

    /// <summary>Removes all cached portraits for the given character.</summary>
    public void Invalidate(string characterId)
    {
        var prefix = characterId + "::";
        var toRemove = new List<string>();
        foreach (var key in _cache.Keys)
        {
            if (key.StartsWith(prefix))
                toRemove.Add(key);
        }
        foreach (var key in toRemove)
            _cache.Remove(key);
    }

    /// <summary>Removes a single cached entry for a specific key combination.</summary>
    public void InvalidateExact(string characterId, string appearanceKey = "", string presetKey = "")
    {
        _cache.Remove(BuildKey(characterId, appearanceKey, presetKey));
    }

    /// <summary>Clears the entire cache.</summary>
    public void InvalidateAll() => _cache.Clear();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string BuildKey(string characterId, string appearanceKey, string presetKey)
        => $"{characterId}::{appearanceKey}::{presetKey}";
}

