using Godot;
using SurveillanceStategodot.scripts.authoring;

namespace SurveillanceStategodot.scripts.presentation;

/// <summary>
/// Presentation-layer Node that maps stable domain IDs back to their authoring
/// resources (e.g. CharacterResource → AvatarScene).
///
/// This is a purely presentation concern — the domain layer never touches it.
/// ScenarioBootstrapper is the single source of truth: it holds the plot
/// definitions and calls Register* here during Init so there is no duplicated
/// export array.
///
/// Usage (designer):
///   1. Place one instance of this node in your main scene (e.g. as a child of
///      the UI root, or as a sibling of PortraitCache).
///   2. Wire an [Export] reference to this node on ScenarioBootstrapper.
///   3. Wire an [Export] reference to this node in any presentation node that
///      needs to resolve a character ID to a CharacterResource (e.g.
///      PortraitTextureRect).
/// </summary>
public partial class ResourceRegistry : Node
{
    private readonly System.Collections.Generic.Dictionary<string, CharacterResource> _charactersById = new();
    private readonly System.Collections.Generic.Dictionary<string, SiteResource> _sitesById = new();

    /// <summary>
    /// Called by ScenarioBootstrapper for every CharacterResource it knows about.
    /// Safe to call multiple times with the same resource.
    /// </summary>
    public void RegisterCharacter(CharacterResource resource)
    {
        _charactersById[resource.CharacterId] = resource;
    }

    /// <summary>
    /// Called by ScenarioBootstrapper for every SiteResource it knows about.
    /// </summary>
    public void RegisterSite(SiteResource resource)
    {
        _sitesById[resource.Id] = resource;
    }

    public bool TryGetCharacter(string characterId, out CharacterResource? resource)
    {
        return _charactersById.TryGetValue(characterId, out resource);
    }

    public bool TryGetSite(string siteId, out SiteResource? resource)
    {
        return _sitesById.TryGetValue(siteId, out resource);
    }
}


