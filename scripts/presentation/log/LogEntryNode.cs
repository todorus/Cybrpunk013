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
            var site = value?.SiteId != null ? WorldState.GetSite(value.SiteId) : null;
            var character = value?.CharacterId != null ? WorldState.GetCharacter(value.CharacterId) : null;
            EmitSignalSiteLabel(site?.Label);
            EmitSignalCharacterLabel(character?.DisplayName);
        }
    }
}