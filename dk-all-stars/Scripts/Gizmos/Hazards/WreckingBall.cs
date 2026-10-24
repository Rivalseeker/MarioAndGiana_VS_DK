using Godot;
using System;

[Tool] 
public partial class WreckingBall : Node2D
{
    [ExportCategory("Pendulum Settings")]
    [Export] public float MaxSwingAngle = 75.0f; 
    [Export] public float SwingSpeed = 2.5f;     
    
    // THE FIX: A 0.0 to 1.0 slider to control exactly where the swing starts!
    // 0.0 = Center (Moving Left)
    // 0.25 = Left Peak
    // 0.5 = Center (Moving Right)
    // 0.75 = Right Peak
    [Export(PropertyHint.Range, "0,1")] 
    public float StartingPhase = 0.0f; 
    
    [Export] public float LimitSwingLeft = 75.0f;  
    [Export] public float LimitSwingRight = 75.0f; 

    [Export] public float ChainLength = 120.0f;  

    [ExportCategory("Node References")]
    [Export] private Node2D _pivot;
    [Export] private Sprite2D _ballSprite; 
    [Export] private Area2D _hitbox;

    [ExportCategory("Debug Visuals")]
    [Export] private Color _arcColor = new Color(1, 0, 0, 0.5f);

    private double _timePassed = 0.0;

    public override void _Ready()
    {
        if (Engine.IsEditorHint()) return; 

        // Convert the 0.0-1.0 slider value into the mathematical time needed to start there
        _timePassed = (StartingPhase * Math.PI * 2.0) / SwingSpeed;

        if (_hitbox != null)
        {
            _hitbox.BodyEntered += OnBodyEntered;
        }
        
        if (_ballSprite != null) _ballSprite.Position = new Vector2(0, ChainLength);
        if (_hitbox != null) _hitbox.Position = new Vector2(0, ChainLength);
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
        {
            QueueRedraw(); 
            
            // This lets you visually preview the exact starting position while building the level.
            if (_pivot != null)
            {
                double editorTime = (StartingPhase * Math.PI * 2.0) / SwingSpeed;
                float rawAngle = (float)Math.Sin(editorTime * SwingSpeed) * MaxSwingAngle;
                float currentAngleDegrees = Mathf.Clamp(rawAngle, -LimitSwingRight, LimitSwingLeft);
                
                _pivot.RotationDegrees = currentAngleDegrees;

                // Keep the ball locked and counter-rotated in the editor view
                if (_ballSprite != null) 
                {
                    _ballSprite.Position = new Vector2(0, ChainLength);
                    _ballSprite.RotationDegrees = -currentAngleDegrees;
                }
                if (_hitbox != null) 
                {
                    _hitbox.Position = new Vector2(0, ChainLength);
                    _hitbox.RotationDegrees = -currentAngleDegrees;
                }
            }
            return;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Engine.IsEditorHint() || _pivot == null) return;

        _timePassed += delta;

        float rawAngle = (float)Math.Sin(_timePassed * SwingSpeed) * MaxSwingAngle;
        float currentAngleDegrees = Mathf.Clamp(rawAngle, -LimitSwingRight, LimitSwingLeft);
        
        _pivot.RotationDegrees = currentAngleDegrees;

        if (_ballSprite != null) _ballSprite.RotationDegrees = -currentAngleDegrees;
        if (_hitbox != null) _hitbox.RotationDegrees = -currentAngleDegrees;
    }

    public override void _Draw()
    {
        if (!Engine.IsEditorHint()) return; 

        float actualLeft = Mathf.Min(MaxSwingAngle, LimitSwingLeft);
        float actualRight = Mathf.Min(MaxSwingAngle, LimitSwingRight);

        float startAngle = Mathf.DegToRad(90 - actualRight);
        float endAngle = Mathf.DegToRad(90 + actualLeft);

        DrawArc(Vector2.Zero, ChainLength, startAngle, endAngle, 32, _arcColor, 2.0f);

        Vector2 rightBound = new Vector2((float)Math.Cos(startAngle), (float)Math.Sin(startAngle)) * ChainLength;
        Vector2 leftBound = new Vector2((float)Math.Cos(endAngle), (float)Math.Sin(endAngle)) * ChainLength;

        DrawLine(Vector2.Zero, rightBound, _arcColor, 1.0f);
        DrawLine(Vector2.Zero, leftBound, _arcColor, 1.0f);
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is BasePlayer player && !player.IsDead)
        {
            if (player.PlayerStateMachine != null)
            {
                player.PlayerStateMachine.ForceTransition("Dead");
            }
            else
            {
                player.Die(); 
            }
        }
    }
}