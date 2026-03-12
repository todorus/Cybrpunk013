using Godot;
using SurveillanceStategodot.scripts.authoring;

namespace SurveillanceStategodot.scripts.presentation.operators;

public partial class OperatorDisplay : Control
{
    [Signal]
    public delegate void OperatorChangedEventHandler(CharacterResource newOperator);
        
    private CharacterResource _operatorResource;
    [Export]
    public CharacterResource OperatorResource
    {
        get => _operatorResource;
        set
        {
            if (value == _operatorResource || value == null) return;
            _operatorResource = value;
            EmitSignalOperatorChanged(value);
        }
    }
    
    public override void _Ready()
    {
        // OperatorResource is assigned during scene deserialization, before signals
        // are connected. Re-emit here so listeners receive the initial value once
        // the scene is fully ready.
        if (_operatorResource != null)
            EmitSignalOperatorChanged(_operatorResource);
    }
}