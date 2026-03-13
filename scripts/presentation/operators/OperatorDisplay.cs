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

    public Texture2D Avatar
    {
        set => EmitSignalAvatarChanged(value);
    }

    public Character Character
    {
        set => EmitSignalNameChanged(value?.DisplayName);
    }

    public void SetAssignment(Assignment? assignment)
    {
        var label = assignment != null ? FormatLabel(assignment) : "Idle";
        EmitSignalAssignmentLabelChanged(label);
    }

    private static string FormatLabel(Assignment assignment) => assignment.Kind switch
    {
        AssignmentKind.TailCharacter  => $"Tailing {assignment.TargetCharacter?.DisplayName ?? "target"}",
        AssignmentKind.StakeoutSite   => $"Staking out {assignment.CurrentOperation?.SiteContext?.Label}",
        AssignmentKind.VisitSite      => $"Visiting: {assignment.CurrentOperation?.SiteContext?.Label}",
        _                             => assignment.CurrentOperation?.Label ?? assignment.Kind.ToString()
    };
}