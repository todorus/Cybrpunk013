using Godot;
using Godot.Collections;
using SurveillanceStategodot.scripts.domain.observation;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.domain.system;
using SurveillanceStategodot.scripts.presentation.portrait;

namespace SurveillanceStategodot.scripts.presentation.log;

public partial class LogEntryNode : Control
{
    [Signal]
    public delegate void SiteLabelEventHandler(string label);
    
    [Signal]
    public delegate void CharacterLabelEventHandler(string label);
    
    [Signal]
    public delegate void AvatarEventHandler(Texture2D avatar);
    
    [Signal]
    public delegate void ActionIconEventHandler(Texture2D icon);
    
    [Signal]
    public delegate void ObservationLabelEventHandler(string label);
    
    [Signal]
    public delegate void BackgroundColorEventHandler(Color color);

    [Export] private Dictionary<ObservationType, Texture2D> _observationTypeIcons;
    
    [Export] private Dictionary<ComplianceType, Color> _complianceTypeColors;

    public WorldState WorldState;
    public PortraitCache PortraitCache;
    public ResourceRegistry ResourceRegistry;

    public ObservationLogKey Key { get; private set; }

    public async void SetObservation(Observation value)
    {
        var site = value?.SiteId != null ? WorldState.GetSite(value.SiteId) : null;
        var character = value?.CharacterId != null ? WorldState.GetCharacter(value.CharacterId) : null;
        if (character != null && ResourceRegistry.TryGetCharacter(character.Id, out var characterResource))
        {
            var avatar = await PortraitCache.GetOrRenderAsync(characterResource);
            EmitSignalAvatar(avatar);
        }

        if (_observationTypeIcons.ContainsKey(value.ObservationType))
        {
            EmitSignalActionIcon(_observationTypeIcons[value.ObservationType]);
        }

        if (_complianceTypeColors.ContainsKey(value.ComplianceType))
        {
            EmitSignalBackgroundColor(_complianceTypeColors[value.ComplianceType]);
        }
        else
        {
            EmitSignalBackgroundColor(Color.FromHsv(0, 0, 0, 0));
        }
        EmitSignalSiteLabel(site?.Label ?? "");
        EmitSignalCharacterLabel(character?.DisplayName ?? "");
        EmitSignalObservationLabel(DescribeObservation(value));
    }

    public async void SetEntry(AggregatedObservationLogEntry entry, WorldState worldState, PortraitCache portraitCache, ResourceRegistry resourceRegistry)
    {
        Key = entry.Key;

        var site = entry.Key.SiteId != null ? worldState.GetSite(entry.Key.SiteId) : null;
        var character = entry.Key.CharacterId != null ? worldState.GetCharacter(entry.Key.CharacterId) : null;

        if (character != null && resourceRegistry.TryGetCharacter(character.Id, out var characterResource))
        {
            var avatar = await portraitCache.GetOrRenderAsync(characterResource);
            EmitSignalAvatar(avatar);
        }

        if (_observationTypeIcons.ContainsKey(entry.ObservationType))
            EmitSignalActionIcon(_observationTypeIcons[entry.ObservationType]);

        if (_complianceTypeColors.ContainsKey(entry.ComplianceType))
            EmitSignalBackgroundColor(_complianceTypeColors[entry.ComplianceType]);
        else
            EmitSignalBackgroundColor(Color.FromHsv(0, 0, 0, 0));

        EmitSignalSiteLabel(site?.Label ?? entry.SiteLabel);
        EmitSignalCharacterLabel(character?.DisplayName ?? entry.CharacterLabel);
        EmitSignalObservationLabel(DescribeObservationType(entry.ObservationType));
    }

    public void RefreshEntry(AggregatedObservationLogEntry entry)
    {
        if (_complianceTypeColors.ContainsKey(entry.ComplianceType))
            EmitSignalBackgroundColor(_complianceTypeColors[entry.ComplianceType]);

        EmitSignalObservationLabel(DescribeObservationType(entry.ObservationType));
    }

    private static string DescribeObservation(Observation? obs)
    {
        if (obs == null) return "";
        return obs.ObservationType switch
        {
            ObservationType.EnteredSite   => "entered site",
            ObservationType.ExitedSite    => "exited site",
            ObservationType.SpottedMoving => "spotted moving",
            _                             => obs.ObservationType.ToString()
        };
    }

    private static string DescribeObservationType(ObservationType type) =>
        type switch
        {
            ObservationType.SpottedMoving => "spotted moving",
            ObservationType.SpottedAtSite => "spotted at site",
            _ => type.ToString()
        };
}