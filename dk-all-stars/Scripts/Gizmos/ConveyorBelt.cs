using Godot;

[Tool]
public partial class ConveyorBelt : StaticBody2D
{
    [ExportCategory("Conveyor Setup")]
    [Export] public int MiddleTileCount = 3; 
    [Export] public float MiddleTileWidth = 8.0f; 
    [Export] public float EndWidth = 8.0f;
    [Export] public float BeltHeight = 8.0f;     

    [ExportCategory("Movement Settings")]
    [Export] public float Speed = 12.0f;
    [Export] public bool IsMovingRight = true;
    
    // Base Animation Speed (Frames per second when Speed is at the baseline of 12.0)
    [Export] public float AnimationFps = 12.0f;

    [ExportCategory("Alternating Settings")]
    [Export] public bool Alternates = false;
    [Export] public float AlternateInterval = 3.0f;

    [ExportCategory("Node References")]
    [Export] private Sprite2D _leftEnd;
    [Export] private TextureRect _middle;
    [Export] private Sprite2D _rightEnd;
    [Export] private CollisionShape2D _collision;
    [Export] private AnimationPlayer _beltEndsAnim;

    private double _directionTimer = 0;
    private double _animationTimer = 0;
    private int _currentFrame = 0;
    private const int TotalMiddleFrames = 8;
    
    // NEW: Baseline speed used to calculate the animation speed ratio
    private const float BaselineSpeed = 12.0f; 

    public override void _Ready()
    {
        UpdateLayout();

        if (Engine.IsEditorHint()) return;
        
        _beltEndsAnim?.Play("Move"); 
        ApplyVelocityAndVisuals();
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
        {
            UpdateLayout();
            return;
        }

        if (Alternates)
        {
            _directionTimer += delta;
            if (_directionTimer >= AlternateInterval)
            {
                _directionTimer = 0;
                IsMovingRight = !IsMovingRight;
                ApplyVelocityAndVisuals();
            }
        }

        AnimateMiddleTiles(delta);
    }

    private void AnimateMiddleTiles(double delta)
    {
        if (_middle == null || !(_middle.Material is ShaderMaterial shaderMat)) return;

        // Prevent division by zero and halt the shader animation if the belt stops
        if (Speed <= 0) return;

        // NEW: Calculate the dynamic FPS based on the current speed relative to the baseline
        float speedRatio = Speed / BaselineSpeed;
        float currentFps = AnimationFps * speedRatio;

        _animationTimer += delta;
        float frameDuration = 1.0f / currentFps;

        if (_animationTimer >= frameDuration)
        {
            // Subtracting the duration instead of setting to 0 keeps the timing mathematically perfect
            _animationTimer -= frameDuration; 
            
            if (IsMovingRight)
            {
                _currentFrame = (_currentFrame + 1) % TotalMiddleFrames;
            }
            else
            {
                _currentFrame = (_currentFrame - 1 + TotalMiddleFrames) % TotalMiddleFrames;
            }

            shaderMat.SetShaderParameter("current_frame", _currentFrame);
        }
    }

    private void ApplyVelocityAndVisuals()
    {
        float direction = IsMovingRight ? 1.0f : -1.0f;
        
        // NEW: Calculate how much faster or slower the belt is moving compared to default
        float speedRatio = Speed / BaselineSpeed;
        
        ConstantLinearVelocity = new Vector2(Speed * direction, 0);

        if (_beltEndsAnim != null)
        {
            // THE FIX: Apply the dynamic speed ratio on top of the direction flip
            _beltEndsAnim.SpeedScale = speedRatio * direction;
        }
    }

    private void UpdateLayout()
    {
        if (_leftEnd == null || _middle == null || _rightEnd == null || _collision == null) return;

        _leftEnd.Position = Vector2.Zero;

        _middle.Position = new Vector2(EndWidth, 0);
        _middle.Size = new Vector2(MiddleTileCount * MiddleTileWidth, BeltHeight);

        if (_middle.Material is ShaderMaterial shaderMat)
        {
            shaderMat.SetShaderParameter("tile_count", (float)MiddleTileCount);
        }

        _rightEnd.Position = new Vector2(EndWidth + (MiddleTileCount * MiddleTileWidth), 0);

        float totalWidth = EndWidth + (MiddleTileCount * MiddleTileWidth) + EndWidth;
        
        if (_collision.Shape is RectangleShape2D rect)
        {
            rect.Size = new Vector2(totalWidth, BeltHeight);
        }
        
        _collision.Position = new Vector2(totalWidth / 2.0f, BeltHeight / 2.0f);
    }

    // ==========================================
    // CUTSCENE CONTROL
    // ==========================================
    
    // NEW: Call this to instantly freeze the belt's physics and visual animations
    public void StopBelt()
    {
        Speed = 0f;
        ApplyVelocityAndVisuals();
    }

    private void Alternate()
    {
        // _directionTimer = 0;
        IsMovingRight = !IsMovingRight;
        ApplyVelocityAndVisuals();
    }
}