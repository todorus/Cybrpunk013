using Godot;
using SurveillanceStategodot.scripts.domain.observation;
using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.presentation.log;

public partial class LogEntryNode : Control
{
    [Signal]
    public delegate void SiteLabelEventHandler(string label);
    
    [Signal]
    public delegate void CharacterLabelEventHandler(string label);
    
    [Signal]
    public delegate void OperationLabelEventHandler(string label);

    public WorldState WorldState;

    public Observation Observation
    {
        set
        {
            var site = WorldState.GetSite(value?.SiteId);
            var character = WorldState.GetCharacter(value?.CharacterId);
            EmitSignalSiteLabel(site.Label);
            EmitSignalCharacterLabel(character.DisplayName);
        }
    }
}