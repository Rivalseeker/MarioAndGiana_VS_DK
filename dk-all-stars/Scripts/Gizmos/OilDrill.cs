using Godot;
using System;

[Tool] // Runs in the editor so you can preview the starting position!
public partial class OilDrill : Node2D
{
    [ExportCategory("Pumpjack Settings")]
    [Export] public float MaxRotationAngle = 20.0f; // How far the beam tilts up and down
    [Export] public float PumpSpeed = 3.0f;         // How fast it pumps
    
    // 0.0 = Level, 0.25 = Tilted fully one way, 0.75 = Tilted fully the other way
    [Export(PropertyHint.Range, "0,1")] 
    public float StartingPhase = 0.0f; 

    [ExportCategory("Node References")]
    [Export] private AnimatableBody2D _headBody;

    private double _timePassed = 0.0;

    public override void _Ready()
    {
        if (Engine.IsEditorHint()) return;

        // Convert the slider value into mathematical time
        _timePassed = (StartingPhase * Math.PI * 2.0) / PumpSpeed;
    }

    public override void _Process(double delta)
    {
        // Visually update the drill's starting position while in the Editor
        if (Engine.IsEditorHint() && _headBody != null)
        {
            double editorTime = (StartingPhase * Math.PI * 2.0) / PumpSpeed;
            _headBody.RotationDegrees = (float)Math.Sin(editorTime * PumpSpeed) * MaxRotationAngle;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Engine.IsEditorHint() || _headBody == null) return;

        _timePassed += delta;

        // Calculate the smooth mechanical bobbing rotation
        float currentAngleDegrees = (float)Math.Sin(_timePassed * PumpSpeed) * MaxRotationAngle;
        
        // Apply it to the AnimatableBody2D!
        _headBody.RotationDegrees = currentAngleDegrees;
    }
}