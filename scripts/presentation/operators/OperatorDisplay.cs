using Godot;
using SurveillanceStategodot.scripts.domain;
using SurveillanceStategodot.scripts.domain.assignment;

namespace SurveillanceStategodot.scripts.presentation.operators;

public partial class OperatorDisplay : Control
{
    [Signal]
    public delegate void AvatarChangedEventHandler(Texture2D newAvatar);

    [Signal]
    public delegate void NameChangedEventHandler(string name);

    [Signal]
    public delegate void AssignmentLabelChangedEventHandler(string label);

    [Signal]
    public delegate void RecallPressedEventHandler(string characterId);
    
    [Signal]
    public delegate void HasAssignmentChangedEventHandler(bool hasAssignment);
    [Signal]
    public delegate void HasNoAssignmentChangedEventHandler(bool hasNoAssignment);

    private string _characterId;

    public Texture2D Avatar
    {
        set => EmitSignalAvatarChanged(value);
    }

    public Character Character
    {
        set
        {
            _characterId = value?.Id;
            EmitSignalNameChanged(value?.DisplayName);
        }
    }

    public void Recall()
    {
        EmitSignalRecallPressed(_characterId);
    }

    public void SetAssignment(Assignment? assignment)
    {
        var label = assignment != null ? FormatLabel(assignment) : "Idle";
        EmitSignalAssignmentLabelChanged(label);
        EmitSignalHasAssignmentChanged(assignment != null);
        EmitSignalHasNoAssignmentChanged(assignment == null);
    }

    private static string FormatLabel(Assignment assignment) => assignment.Kind switch
    {
        AssignmentKind.TailCharacter  => $"Tailing {assignment.TargetCharacter?.DisplayName ?? "target"}",
        AssignmentKind.StakeoutSite   => $"Staking out {assignment.CurrentOperation?.SiteContext?.Label}",
        AssignmentKind.VisitSite      => $"Visiting: {assignment.CurrentOperation?.SiteContext?.Label}",
        _                             => assignment.CurrentOperation?.Label ?? assignment.Kind.ToString()
    };
}