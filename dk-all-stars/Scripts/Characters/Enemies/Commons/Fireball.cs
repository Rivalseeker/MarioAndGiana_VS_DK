using Godot;

public partial class Fireball : BaseEnemy
{
    // Palette Colors
    private static readonly Color DefaultColor1 = new Color("#f83800");
    private static readonly Color DefaultColor2 = new Color("#fce0a8");
    private static readonly Color DefaultColor3 = new Color("#fcfcfc");

    private static readonly Color HammerColor1 = new Color("#61d3e3");
    private static readonly Color HammerColor2 = new Color("#9241f3");
    private static readonly Color HammerColor3 = new Color("#f83800");

    [ExportCategory("Movement Stats")]
    [Export] public float WalkSpeed = 30.0f;
    [Export] public float ClimbSpeed = 25.0f;
    [Export] public float MaxStepHeight = 4.0f;

    [ExportCategory("State Timers")]
    [Export] public float MinIdleDuration = 0.1f;
    [Export] public float MaxIdleDuration = 0.5f;
    [Export] public float MinMoveDuration = 1.0f;
    [Export] public float MaxMoveDuration = 3.0f;

    [ExportCategory("RayCasts & Areas")]
    [Export] public Area2D Hitbox;
    [Export] public RayCast2D FloorDetectLeft;
    [Export] public RayCast2D FloorDetectRight;
    [Export] public RayCast2D LeftLadderDetector;
    [Export] public RayCast2D RightLadderDetector;
    [Export] public RayCast2D LeftLedgeDetector;
    [Export] public RayCast2D RightLedgeDetector;
    [Export] public RayCast2D ClimbLadderDetector;
    [Export] public RayCast2D ClimbLedgeDetector;
    [Export] public RayCast2D StepDetector;

    [ExportCategory("Oil Reaction")]
    [Export] public float OiledDuration = 3.0f;
    [Export] public float OiledScaleTime = 0.3f;
    public StringName PreOiledState { get; set; }

    [Export] public AudioStreamPlayer _sfxHit;
    [Export] public EnemyStateMachine StateMachine;

    public int FacingDirection { get; set; } = 1; 
    public float CurrentClimbDirection { get; set; } = 0f; 
    public bool IsClimbing { get; set; } = false;
    public bool JustSteppedUp { get; private set; } = false;
    public Vector2 SpawnLeapVelocity { get; set; } = Vector2.Zero;
    public bool UseCustomGravity { get; set; } = false;

    public override void _Ready()
    {
        FloorSnapLength = MaxStepHeight;

        if (Hitbox != null)
        {
            Hitbox.AreaEntered += OnPlayerHurtAreaEntered;
        }

        // Ensure materials are local to this instance so shader parameter changes don't bleed
        EnsureUniqueMaterials();

        // Check if the Hammer is active when spawned
        if (GetLevel() is BaseDkLevel level && level.IsHammerActive)
        {
            SetHammerPalette(true);
        }
    }

    // ==========================================
    // PALETTE & SHADER RECOLOR LOGIC
    // ==========================================
    public void SetHammerPalette(bool active)
    {
        Color c1 = active ? HammerColor1 : DefaultColor1;
        Color c2 = active ? HammerColor2 : DefaultColor2;
        Color c3 = active ? HammerColor3 : DefaultColor3;

        ApplyShaderParameters("FireballSprite1", c1, c2, c3);
        ApplyShaderParameters("FireballSprite2", c1, c2, c3);
    }

    private void EnsureUniqueMaterials()
    {
        EnsureUniqueMaterial("FireballSprite1");
        EnsureUniqueMaterial("FireballSprite2");
    }

    private void EnsureUniqueMaterial(string spriteName)
    {
        if (GetNodeOrNull<Sprite2D>(spriteName) is Sprite2D sprite && sprite.Material is ShaderMaterial mat)
        {
            sprite.Material = (ShaderMaterial)mat.Duplicate();
        }
    }

    private void ApplyShaderParameters(string spriteName, Color c1, Color c2, Color c3)
    {
        if (GetNodeOrNull<Sprite2D>(spriteName) is Sprite2D sprite && sprite.Material is ShaderMaterial mat)
        {
            mat.SetShaderParameter("color_1_replace", c1);
            mat.SetShaderParameter("color_2_replace", c2);
            mat.SetShaderParameter("color_3_replace", c3);
        }
    }

    public BaseDkLevel GetLevel()
    {
        Node current = this;
        while (current != null)
        {
            if (current is BaseDkLevel level) return level;
            current = current.GetParent();
        }
        return null;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsClimbing && !UseCustomGravity)
        {
            Vector2 velocity = Velocity;
            if (!IsOnFloor())
            {
                velocity.Y += FallGravity * (float)delta;
            }
            Velocity = velocity;
        }

        if (FacingDirection != 0 && GetNodeOrNull<Sprite2D>("FireballSprite1") is Sprite2D sprite)
        {
            sprite.FlipH = FacingDirection < 0;
        }

        if (FacingDirection != 0 && GetNodeOrNull<Sprite2D>("FireballSprite2") is Sprite2D sprite2)
        {
            sprite2.FlipH = FacingDirection < 0;
        }

        if (StepDetector != null && FacingDirection != 0)
        {
            StepDetector.TargetPosition = new Vector2(Mathf.Abs(StepDetector.TargetPosition.X) * FacingDirection, 0);
        }
        
        MoveAndSlide();

        if (!IsClimbing)
        {
            HandleStepUp();
        }
    }

    private void HandleStepUp()
    {
        JustSteppedUp = false;

        if (!IsOnFloor() || StepDetector == null || !StepDetector.IsColliding()) return;
        if (FacingDirection == 0) return;

        Vector2 forwardStep = new Vector2(FacingDirection * 2.0f, 0);
        Vector2 upStep = new Vector2(0, -MaxStepHeight);

        if (!TestMove(GlobalTransform, upStep))
        {
            Transform2D raisedTransform = GlobalTransform.Translated(upStep);
            
            if (!TestMove(raisedTransform, forwardStep))
            {
                Transform2D raisedForwardTransform = raisedTransform.Translated(forwardStep);
                KinematicCollision2D collision = new KinematicCollision2D();
                
                if (TestMove(raisedForwardTransform, new Vector2(0, MaxStepHeight), collision))
                {
                    float stepLift = MaxStepHeight - collision.GetTravel().Y;
                    GlobalPosition += new Vector2(0, -stepLift);
                    Velocity = new Vector2(FacingDirection * WalkSpeed, Velocity.Y); 
                    JustSteppedUp = true;
                }
            }
        }
    }

    private void OnPlayerHurtAreaEntered(Area2D area)
    {
        if (area.Name == "PlayerHurtBox")
        {
            BasePlayer targetPlayer = area.GetParent() as BasePlayer;

            if (!targetPlayer.IsDead)
            {
                targetPlayer.Die();
            }
        }
    }

    public void ReactToOil()
    {
        if (StateMachine != null && StateMachine.CurrentState != null)
        {
            PreOiledState = StateMachine.CurrentState.Name;
            StateMachine.ForceTransition("FireballOiled");
        }
    }

    public BasePlayer GetTargetPlayer()
    {
        Node current = this;
        while (current != null)
        {
            if (current is BaseDkLevel level)
            {
                return level._activePlayer;
            }
            current = current.GetParent();
        }
        return null;
    }
}






// using Godot;

// public partial class Fireball : BaseEnemy
// {
//     [ExportCategory("Movement Stats")]
//     [Export] public float WalkSpeed = 30.0f;
//     [Export] public float ClimbSpeed = 25.0f;
//     [Export] public float MaxStepHeight = 4.0f; // NEW: Defines the step height

//     [ExportCategory("State Timers")]
//     [Export] public float MinIdleDuration = 0.1f;
//     [Export] public float MaxIdleDuration = 0.5f;
//     [Export] public float MinMoveDuration = 1.0f;
//     [Export] public float MaxMoveDuration = 3.0f;

//     [ExportCategory("RayCasts & Areas")]
//     [Export] public Area2D Hitbox; // NEW: Drag your Hitbox node here
//     [Export] public RayCast2D FloorDetectLeft;
//     [Export] public RayCast2D FloorDetectRight;
//     [Export] public RayCast2D LeftLadderDetector;
//     [Export] public RayCast2D RightLadderDetector;
//     [Export] public RayCast2D LeftLedgeDetector;
//     [Export] public RayCast2D RightLedgeDetector;
//     [Export] public RayCast2D ClimbLadderDetector;
//     [Export] public RayCast2D ClimbLedgeDetector;
//     [Export] public RayCast2D StepDetector; // NEW

//     [ExportCategory("Oil Reaction")]
//     [Export] public float OiledDuration = 3.0f; // How long it stays large
//     [Export] public float OiledScaleTime = 0.3f; // How fast it smoothly grows/shrinks
//     // Add this to store the state memory
//     public StringName PreOiledState { get; set; }

//     [Export] public AudioStreamPlayer _sfxHit;

//     [Export] public EnemyStateMachine StateMachine;

//     // Public State Variables 
//     public int FacingDirection { get; set; } = 1; 
//     public float CurrentClimbDirection { get; set; } = 0f; 
//     public bool IsClimbing { get; set; } = false;
    
//     // NEW: Tells the GroundMove state if we just climbed a tiny step so it doesn't turn around!
//     public bool JustSteppedUp { get; private set; } = false;
//     // NEW: Memory variable to store the leap velocity before the state machine activates
//     public Vector2 SpawnLeapVelocity { get; set; } = Vector2.Zero;
//     // NEW: Allows individual states (like FireballSpawn) to bypass the default enemy gravity
//     public bool UseCustomGravity { get; set; } = false;

//     public override void _Ready()
//     {
//         // THE STEP DOWN FIX: Automatically snaps the Fireball to the floor on 4px drops!
//         FloorSnapLength = MaxStepHeight;

//         // NEW: Wire up the Hitbox to listen for Mario!
//         if (Hitbox != null)
//         {
//             Hitbox.AreaEntered += OnPlayerHurtAreaEntered;
//         }
//     }

//     public override void _PhysicsProcess(double delta)
//     {
//         // UPDATE: Only apply generic gravity if not climbing and custom gravity is disabled
//         if (!IsClimbing && !UseCustomGravity)
//         {
//             Vector2 velocity = Velocity;
//             if (!IsOnFloor())
//             {
//                 velocity.Y += FallGravity * (float)delta;
//             }
//             Velocity = velocity;
//         }

//         // if (!IsClimbing)
//         // {
//         //     Vector2 velocity = Velocity;
//         //     if (!IsOnFloor())
//         //     {
//         //         velocity.Y += FallGravity * (float)delta;
//         //     }
//         //     Velocity = velocity;
//         // }

//         if (FacingDirection != 0 && GetNodeOrNull<Sprite2D>("FireballSprite1") is Sprite2D sprite)
//         {
//             sprite.FlipH = FacingDirection < 0;
//         }

//         if (FacingDirection != 0 && GetNodeOrNull<Sprite2D>("FireballSprite2") is Sprite2D sprite2)
//         {
//             sprite2.FlipH = FacingDirection < 0;
//         }

//         // NEW: Always point the StepDetector in the direction the Fireball is walking
//         if (StepDetector != null && FacingDirection != 0)
//         {
//             StepDetector.TargetPosition = new Vector2(Mathf.Abs(StepDetector.TargetPosition.X) * FacingDirection, 0);
//         }
        
//         MoveAndSlide();

//         // NEW: Run the step up math immediately after physics movement
//         if (!IsClimbing)
//         {
//             HandleStepUp();
//         }
//     }

//     private void HandleStepUp()
//     {
//         JustSteppedUp = false;

//         // If not on the floor, or the toe isn't touching a wall, skip!
//         if (!IsOnFloor() || StepDetector == null || !StepDetector.IsColliding()) return;

//         if (FacingDirection == 0) return;

//         Vector2 forwardStep = new Vector2(FacingDirection * 2.0f, 0);
//         Vector2 upStep = new Vector2(0, -MaxStepHeight);

//         // 1. Check if moving UP hits a ceiling
//         if (!TestMove(GlobalTransform, upStep))
//         {
//             Transform2D raisedTransform = GlobalTransform.Translated(upStep);
            
//             // 2. Check if moving FORWARD from the raised position is clear
//             if (!TestMove(raisedTransform, forwardStep))
//             {
//                 Transform2D raisedForwardTransform = raisedTransform.Translated(forwardStep);
//                 KinematicCollision2D collision = new KinematicCollision2D();
                
//                 // 3. Check where the exact floor of the step is by casting down
//                 if (TestMove(raisedForwardTransform, new Vector2(0, MaxStepHeight), collision))
//                 {
//                     float stepLift = MaxStepHeight - collision.GetTravel().Y;
//                     GlobalPosition += new Vector2(0, -stepLift);
                    
//                     // Maintain horizontal momentum
//                     Velocity = new Vector2(FacingDirection * WalkSpeed, Velocity.Y); 
//                     JustSteppedUp = true;
//                 }
//             }
//         }
//     }

//     // NEW: Trigger the player's death sequence on contact
//     private void OnPlayerHurtAreaEntered(Area2D area)
//     {
//         if (area.Name == "PlayerHurtBox")
//         {
//             // player && !player.IsDead
//             BasePlayer targetPlayer = area.GetParent() as BasePlayer;

//             if (!targetPlayer.IsDead)
//             {
//                 targetPlayer.Die();
//             }
//         }
//     }

//     // ==========================================
//     // OIL REACTION LOGIC
//     // ==========================================
//     public void ReactToOil()
//     {
//         if (StateMachine != null && StateMachine.CurrentState != null)
//         {
//             // THE FIX: Memorize the exact state the Fireball is in before forcing the transition!
//             PreOiledState = StateMachine.CurrentState.Name;
            
//             StateMachine.ForceTransition("FireballOiled");
//         }
//     }

//     // ==========================================
//     // AI HELPERS
//     // ==========================================
    
//     // NEW: Safely traverses up the tree to find the active player from the Level
//     public BasePlayer GetTargetPlayer()
//     {
//         Node current = this;
//         while (current != null)
//         {
//             if (current is BaseDkLevel level)
//             {
//                 return level._activePlayer;
//             }
//             current = current.GetParent();
//         }
//         return null;
//     }
// }