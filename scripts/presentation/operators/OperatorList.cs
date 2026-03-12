using Godot;
using SurveillanceStategodot.scripts.authoring;
using SurveillanceStategodot.scripts.presentation.portrait;

namespace SurveillanceStategodot.scripts.presentation.operators;

public partial class OperatorList : Container
{
    [Export]
    private PortraitCache _portraitCache = null!;

    [Export] 
    private PackedScene _operatorScene;

    [Export] 
    private CharacterResource[] _operators = [];
}