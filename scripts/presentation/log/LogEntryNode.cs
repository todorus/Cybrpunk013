using Godot;
using Godot.Collections;
using SurveillanceStategodot.scripts.domain.observation;
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

    [Export] private Dictionary<ObservationType, Texture2D> _observationTypeIcons;

    public WorldState WorldState;
    public PortraitCache PortraitCache;
    public ResourceRegistry ResourceRegistry;

    public Observation Observation
    {
        set
        {
            var site = value?.SiteId != null ? WorldState.GetSite(value.SiteId) : null;
            var character = value?.CharacterId != null ? WorldState.GetCharacter(value.CharacterId) : null;
            if (ResourceRegistry.TryGetCharacter(character.Id, out var characterResource))
            {
                var avatar = PortraitCache.GetOrRenderAsync(characterResource).Result;
                EmitSignalAvatar(avatar);
            }

            if (_observationTypeIcons.ContainsKey(value.ObservationType))
            {
                EmitSignalActionIcon(_observationTypeIcons[value.ObservationType]);
            }


            EmitSignalSiteLabel(site?.Label ?? "");
            EmitSignalCharacterLabel(character?.DisplayName ?? "");
            EmitSignalObservationLabel(DescribeObservation(value));
        }
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
}