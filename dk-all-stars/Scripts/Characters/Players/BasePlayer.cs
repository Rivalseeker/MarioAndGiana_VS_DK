using Godot;
using System;

public partial class BasePlayer : CharacterBody2D
{
    // ==========================================
    // DEATH SEQUENCE SIGNALS & ANIMATION HOOKS
    // ==========================================
    [Signal] public delegate void PlayerDiedEventHandler();
    [Signal] public delegate void AllowDeathSkipEventHandler();
    [Signal] public delegate void PlayFailureMusicEventHandler();
    [Signal] public delegate void TriggerTooBadPromptEventHandler();
    [Signal] public delegate void DeathAnimationFinishedEventHandler();
    [Signal] public delegate void LandAnimationFinishedEventHandler();

    public void AnimEvent_LandAnimationFinished() => EmitSignal(SignalName.LandAnimationFinished);
    public void AnimEvent_AllowDeathSkip() => EmitSignal(SignalName.AllowDeathSkip);
    public void AnimEvent_PlayFailureMusic() => EmitSignal(SignalName.PlayFailureMusic);
    public void AnimEvent_TriggerTooBadPrompt() => EmitSignal(SignalName.TriggerTooBadPrompt);
    public void AnimEvent_DeathAnimationFinished() => EmitSignal(SignalName.DeathAnimationFinished);

    // ========================================
    // MOVEMENT PARAMETERS
    // ========================================

    [ExportCategory("Movement Stats")]
    [Export] public float WalkSpeed = 60.0f;
    [Export] public float ClimbSpeed = 40.0f;
    
    // NEW: Defines both the max step up height and max step down snap distance!
    [Export] public float MaxStepHeight = 4.0f; 
    
    [Export] public int MaxJumps = 1; 

    // ==========================================
    // KINEMATIC JUMP PARAMETERS
    // ==========================================
    [ExportCategory("Jump Mechanics")]
    [Export] public float JumpHeight = 48.0f; 
    [Export] public float JumpTimeToPeak = 0.4f;
    [Export] public float JumpTimeToDescent = 0.3f; 

    public float JumpVelocity { get; private set; }
    public float JumpGravity { get; private set; }
    public float FallGravity { get; private set; }

    // ==========================================
    // PUBLIC STATE VARIABLES
    // ==========================================
    public int CurrentJumps { get; set; } = 0;
    public int FacingDirection { get; set; } = 1; 
    public bool IsClimbing { get; set; } = false;
    public bool IsInPit { get; set; } = false;
    public bool IsDead { get; set; } = false;
    public bool IsIdle { get; set; } = false;
    public bool IsGroundMove { get; set; } = false;
    public bool JustFinishedClimbing { get; set; } = false;
    public bool IsSpinJumping { get; set; } = false; // NEW

    // ==========================================
    // LADDER STATE 
    // ==========================================
    public bool CanClimb { get; set; } = false;
    public bool CanClimbDownLedge { get; set; } = false;
    public float CurrentLadderX { get; set; } = 0f;
    public float CurrentLadderTopY { get; set; } = 0f;
    public float CurrentLadderBottomY { get; set; } = 0f;
    public float CurrentLadderVelocityY { get; set; } = 0f;
    public bool CurrentLadderHasLedge { get; set; } = false;

    // ==========================================
    // NEW: CLIMB STATE TRANSFER VARIABLES
    // ==========================================
    public string TargetClimbAnim { get; set; } = "Climb";
    public bool TargetClimbPlayBackwards { get; set; } = false;

    // ==========================================
    // NODE REFERENCES
    // ==========================================
    [ExportCategory("Visuals")]
    [Export] public Sprite2D _playerSprite;
    [Export] public Sprite2D PlayerLandSprite;
    [Export] private AnimationPlayer _locatorAnim;

    [ExportCategory("Ladder Detection")]
    [Export] public RayCast2D LadderDetector;
    [Export] public RayCast2D LedgeDetector; // RENAMED FROM DownLedgeDetector
    [Export] public RayCast2D ChestLedgeDetector; // NEW
    [Export] public RayCast2D StepDetector;

    [ExportCategory("Audio")]
    [Export] private AudioStreamPlayer _sfxGroundMove;
    [Export] private AudioStreamPlayer _sfxJump;
    [Export] private AudioStreamPlayer _sfxFall;
    [Export] private AudioStreamPlayer _sfxHit;

    [Export] public AnimationPlayer PlayerAnim;

    [Export] public StateMachine PlayerStateMachine; // NEW

    // ==========================================
    // HAMMER & WEAPON STATE
    // ==========================================
    public bool HasHammer { get; set; } = false;
    public bool HasGoldenHammer { get; set; } = false;

    [ExportCategory("Hammer Sprites")]
    [Export] public Sprite2D PlayerSpriteHammerArms1;
    [Export] public Sprite2D PlayerSpriteHammerArms2;
    [Export] public Sprite2D PlayerSpriteHammerArms3;
    [Export] public AnimationPlayer PlayerHammerAnim;

    // ==========================================
    // HAZARD SETTINGS
    // ==========================================
    [ExportCategory("Hazards")]
    [Export] public float OilStainDuration = 3.0f; // Developer-specified duration

    private double _oilStainTimer = 0;
    private double _landingTimer = 0;




    public override void _Ready()
    {
        CalculateJumpPhysics();
        _locatorAnim?.Play("Blink");

        // THE STEP DOWN FIX: 
        // Tells MoveAndSlide to automatically keep Mario on the floor 
        // if he walks down a ledge that is 4 pixels or smaller!
        FloorSnapLength = MaxStepHeight;

        // THE MOVING PLATFORM FIX:
        // Prevents moving platforms from adding their velocity to Mario's jump arc
        PlatformOnLeave = PlatformOnLeaveEnum.DoNothing;
    }


    public override void _Process(double delta)
{
    // NEW: Landing Timer Logic
    if (_landingTimer > 0)
    {
        // Revert immediately if the hammer is grabbed during the 0.1s window
        if (HasHammer || HasGoldenHammer)
        {
            RevertLandingVisual();
        }
        else
        {
            _landingTimer -= delta;
            
            // Revert once 0.1 seconds passes
            if (_landingTimer <= 0)
            {
                RevertLandingVisual();
            }
        }
    }

    // ARM VISIBILITY LOGIC (Keep your existing code here)
    if ((HasHammer || HasGoldenHammer) && PlayerHammerAnim != null && (PlayerHammerAnim.AssignedAnimation == "Hammer" || PlayerHammerAnim.AssignedAnimation == "GoldenHammer"))
    {
        int frame = _playerSprite.Frame;
        
        if (PlayerSpriteHammerArms1 != null) PlayerSpriteHammerArms1.Visible = (frame == 111);
        if (PlayerSpriteHammerArms2 != null) PlayerSpriteHammerArms2.Visible = (frame == 129);
        if (PlayerSpriteHammerArms3 != null) PlayerSpriteHammerArms3.Visible = (frame == 147);
    }
    else
    {
        if (PlayerSpriteHammerArms1 != null) PlayerSpriteHammerArms1.Visible = false;
        if (PlayerSpriteHammerArms2 != null) PlayerSpriteHammerArms2.Visible = false;
        if (PlayerSpriteHammerArms3 != null) PlayerSpriteHammerArms3.Visible = false;
    }

    // OIL STAIN LOGIC (Keep your existing code here)
    if (_oilStainTimer > 0)
    {
        _oilStainTimer -= delta;
        if (_oilStainTimer <= 0)
        {
            SetSpriteColor(Colors.White);
        }
    }
}




    private void CalculateJumpPhysics()
    {
        JumpVelocity = ((2.0f * JumpHeight) / JumpTimeToPeak) * -1.0f;
        JumpGravity = (2.0f * JumpHeight) / (JumpTimeToPeak * JumpTimeToPeak);
        FallGravity = (2.0f * JumpHeight) / (JumpTimeToDescent * JumpTimeToDescent);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsDead) return;

        Vector2 velocity = Velocity;

        if (!IsOnFloor() && !IsClimbing)
        {
            float currentGravity = velocity.Y < 0 ? JumpGravity : FallGravity;
            velocity.Y += currentGravity * (float)delta;
        }

        Velocity = velocity;
        MoveAndSlide();

        // THE LADDER FIX: Override moving platform influence while climbing
        if (IsClimbing)
        {
            Velocity = new Vector2(0, Velocity.Y);
            GlobalPosition = new Vector2(
                CurrentLadderX, 
                GlobalPosition.Y + (CurrentLadderVelocityY * (float)delta)
            );
        }

        // THE FIX: Check if the player was pinched by moving objects!
        CheckForCrush();

        // THE STEP UP FIX:
        // Runs immediately after MoveAndSlide hits a wall
        HandleStepUp();
    }




    private void HandleStepUp()
    {
        // THE FIX: Instead of checking IsOnWall(), we check if the toe RayCast hit the one-way ledge!
        if (!IsOnFloor() || StepDetector == null || !StepDetector.IsColliding()) return;

        // Ignore moving platforms so StepDetector never lifts the player off an AnimatableBody2D
        if (StepDetector.GetCollider() is AnimatableBody2D) return;

        float moveDir = Input.GetAxis("Left", "Right");
        if (moveDir == 0) return;

        Vector2 forwardStep = new Vector2(moveDir * 2.0f, 0);
        Vector2 upStep = new Vector2(0, -MaxStepHeight);

        // 1. Check if moving UP hits a ceiling (TestMove completely ignores one-way platforms when moving up!)
        if (!TestMove(GlobalTransform, upStep))
        {
            Transform2D raisedTransform = GlobalTransform.Translated(upStep);
            
            // 2. Check if moving FORWARD from the raised position is clear
            if (!TestMove(raisedTransform, forwardStep))
            {
                Transform2D raisedForwardTransform = raisedTransform.Translated(forwardStep);
                KinematicCollision2D collision = new KinematicCollision2D();
                
                // 3. Check where the exact floor of the step is by casting down
                if (TestMove(raisedForwardTransform, new Vector2(0, MaxStepHeight), collision))
                {
                    float stepLift = MaxStepHeight - collision.GetTravel().Y;
                    GlobalPosition += new Vector2(0, -stepLift);
                    Velocity = new Vector2(moveDir * WalkSpeed, Velocity.Y);
                    
                    // Re-snap to the floor immediately after modifying GlobalPosition
                    ApplyFloorSnap();
                }
            }
        }
    }


    private void CheckForCrush()
    {
        bool touchingFloor = false;
        bool touchingCeiling = false;
        bool touchingLeft = false;
        bool touchingRight = false;

        // Loop through everything the player physically touched this frame
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            KinematicCollision2D collision = GetSlideCollision(i);
            var collider = collision.GetCollider();
            bool isSolidCrushSurface = false;

            if (collider is PhysicsBody2D physicsBody)
            {
                // For normal bodies (like your Oil Drill), keep the standard Layer 1 (Bit 0) check
                if ((physicsBody.CollisionLayer & 1) != 0)
                {
                    isSolidCrushSurface = true;
                }
            }
            else if (collider is TileMapLayer tileMap)
            {
                // THE FIX: For TileMaps, find the exact tile cell we just collided with.
                // We subtract the normal to push the coordinate 2 pixels *into* the wall/floor
                Vector2 hitPoint = collision.GetPosition() - (collision.GetNormal() * 2.0f);
                Vector2I cell = tileMap.LocalToMap(tileMap.ToLocal(hitPoint));
                TileData data = tileMap.GetCellTileData(cell);

                // Check if this specific tile has a collision polygon on Physics Layer Element 0!
                if (data != null && data.GetCollisionPolygonsCount(0) > 0)
                {
                    isSolidCrushSurface = true;
                }
            }

            // Only process the crushing direction if the surface passed the checks above
            if (isSolidCrushSurface)
            {
                Vector2 normal = collision.GetNormal();
                
                if (normal.Y < -0.5f) touchingFloor = true;
                if (normal.Y > 0.5f) touchingCeiling = true;
                if (normal.X > 0.5f) touchingLeft = true;
                if (normal.X < -0.5f) touchingRight = true;
            }
        }

        // If pinched between opposing solid forces, trigger the death state!
        if ((touchingFloor && touchingCeiling) || (touchingLeft && touchingRight))
        {
            Die();
        }
    }


    // ==========================================
    // HELPER METHODS
    // ==========================================

    // NEW: Uses the tip of a RayCast to check for Custom Tile Data, bypassing physics!
    // NEW: Uses pure Physics collision to read Tile Custom Data!
    public bool GetRaycastCustomData(RayCast2D ray, string dataName)
    {
        if (ray == null || !ray.IsColliding()) return false;

        if (ray.GetCollider() is TileMapLayer layer)
        {
            // Push the detection point 2 pixels forward along the ray's path to guarantee 
            // we are looking exactly inside the tile that we collided with.
            Vector2 hitPoint = ray.GetCollisionPoint() + (ray.TargetPosition.Normalized() * 2.0f);
            
            Vector2I cell = layer.LocalToMap(layer.ToLocal(hitPoint));
            TileData data = layer.GetCellTileData(cell);
            
            if (data != null)
            {
                Variant customData = data.GetCustomData(dataName);
                if (customData.VariantType == Variant.Type.Bool) return customData.AsBool();
            }
        }
        return false;
    }

    // NEW: Uses physics to find the exact horizontal center of the ladder tile
    public void CenterOnLadderX(RayCast2D ray)
    {
        if (ray != null && ray.IsColliding() && ray.GetCollider() is TileMapLayer layer)
        {
            Vector2 hitPoint = ray.GetCollisionPoint() + (ray.TargetPosition.Normalized() * 2.0f);
            Vector2I cell = layer.LocalToMap(layer.ToLocal(hitPoint));
            
            // Get the exact center coordinate of the tile map cell
            Vector2 localCenter = layer.MapToLocal(cell);
            float globalX = layer.ToGlobal(localCenter).X;
            
            GlobalPosition = new Vector2(globalX, GlobalPosition.Y);
        }
    }

    public void UpdateFacingDirection(float inputX)
    {
        if (inputX > 0)
        {
            FacingDirection = 1;
            _playerSprite.FlipH = false;
            PlayerLandSprite.FlipH = false;
            FlipHammerSprites(false);
        }
        else if (inputX < 0)
        {
            FacingDirection = -1;
            _playerSprite.FlipH = true;
            PlayerLandSprite.FlipH = true;
            FlipHammerSprites(true);
        }

        if (StepDetector != null)
        {
            StepDetector.TargetPosition = new Vector2(Mathf.Abs(StepDetector.TargetPosition.X) * FacingDirection, 0);
        }
    }

    private void FlipHammerSprites(bool flip)
    {
        if (PlayerSpriteHammerArms1 != null) PlayerSpriteHammerArms1.FlipH = flip;
        if (PlayerSpriteHammerArms2 != null) PlayerSpriteHammerArms2.FlipH = flip;
        if (PlayerSpriteHammerArms3 != null) PlayerSpriteHammerArms3.FlipH = flip;
    }

    public void TriggerLandingVisual()
    {
        // Do not occur if wielding a hammer
        if (HasHammer || HasGoldenHammer) return; 
        
        _landingTimer = 0.1;
        if (_playerSprite != null) _playerSprite.Visible = false;
        if (PlayerLandSprite != null) PlayerLandSprite.Visible = true;
    }

    public void RevertLandingVisual()
    {
        _landingTimer = 0;
        if (_playerSprite != null) _playerSprite.Visible = true;
        if (PlayerLandSprite != null) PlayerLandSprite.Visible = false;
    }

    // ==========================================
    // OIL SPLATTER LOGIC
    // ==========================================
    public void ApplyOilStain()
    {
        // 1. Apply the visual effect to Mario
        _oilStainTimer = OilStainDuration;
        SetSpriteColor(new Color("#3c3c3c"));

        // 2. THE FIX: Search the tree for the CanvasLayer and trigger the screen splatter!
        Node current = this;
        while (current != null)
        {
            if (current.HasNode("OilSplatterScreen"))
            {
                Node screen = current.GetNode("OilSplatterScreen");
                if (screen.HasMethod("SpawnScreenSplatter"))
                {
                    screen.Call("SpawnScreenSplatter");
                    break;
                }
            }
            current = current.GetParent();
        }
        _sfxHit.Play();
    }

    private void SetSpriteColor(Color color)
    {
        // Safely tint the main sprite and all potential hammer arms!
        if (_playerSprite != null) _playerSprite.Modulate = color;
        if (PlayerLandSprite != null) PlayerLandSprite.Modulate = color;
        if (PlayerSpriteHammerArms1 != null) PlayerSpriteHammerArms1.Modulate = color;
        if (PlayerSpriteHammerArms2 != null) PlayerSpriteHammerArms2.Modulate = color;
        if (PlayerSpriteHammerArms3 != null) PlayerSpriteHammerArms3.Modulate = color;
    }

    // ==========================================
    // DEATH LOGIC
    // ==========================================
    public void Die()
    {
        if (IsDead) return; 

        // THE FIX: Route the death command directly into the State Machine
        // so it cleanly exits the active state (like climbing or running)
        if (PlayerStateMachine != null)
        {
            PlayerStateMachine.ForceTransition("Dead");
        }
        else
        {
            // Fallback just in case the state machine isn't assigned
            IsDead = true;
            Velocity = Vector2.Zero;
            PlayAnimation("Dead"); 
            EmitSignal(SignalName.PlayerDied);
        }
    }

    public void PlaySfx(string sfxName)
    {
        switch (sfxName)
        {
            case "GroundMove": 
                if (_sfxGroundMove != null)
                {
                    float[] pitches = { 0.9f, 0.95f, 1.0f };
                    int randomIndex = GD.RandRange(0, 2); 
                    _sfxGroundMove.PitchScale = pitches[randomIndex];
                    _sfxGroundMove.Play(); 
                }
                break;
            case "Jump": _sfxJump?.Play(); break;
            case "Fall": _sfxFall?.Play(); break;
            case "Hit": _sfxHit?.Play(); break;
            default: GD.PrintErr($"SFX {sfxName} not found on BasePlayer."); break;
        }
    }

    // ==========================================
    // ANIMATION CONTROL
    // ==========================================
    public void PlayAnimation(string animName)
    {
        if (HasHammer || HasGoldenHammer)
        {
            // Map "Idle" to "GroundMoveHammer" since there is no separate "IdleHammer" animation
            string hammerAnim = (animName == "Idle") ? "GroundMoveHammer" : animName + "Hammer";
            string swingAnim = HasGoldenHammer ? "GoldenHammer" : "Hammer";
            
            if (animName == "Jump" || animName == "Fall")
            {
                // Stop it entirely in the air. This guarantees it restarts from 0 when hitting the ground!
                PlayerHammerAnim?.Stop();
            }
            else if (animName == "GroundMove" || animName == "Idle")
            {
                if (PlayerHammerAnim != null && PlayerHammerAnim.HasAnimation(swingAnim))
                {
                    // If it was just paused in Idle, this safely resumes it.
                    PlayerHammerAnim.Play(swingAnim);
                }
            }

            if (PlayerAnim != null && PlayerAnim.HasAnimation(hammerAnim))
            {
                PlayerAnim.Play(hammerAnim);
                
                // When entering Idle with a hammer, start at 0.3s and pause the legs
                if (animName == "Idle")
                {
                    PlayerAnim.Seek(0.3, true);
                    PauseAnimations(); 
                }
                
                return;
            }
        }

        if (PlayerAnim != null && PlayerAnim.HasAnimation(animName))
        {
            PlayerAnim.Play(animName);
        }
    }

    // // ==========================================
    // // ANIMATION CONTROL
    // // ==========================================
    // public void PlayAnimation(string animName)
    // {
    //     if (HasHammer)
    //     {
    //         string hammerAnim = animName + "Hammer";
            
    //         if (animName == "Jump" || animName == "Fall")
    //         {
    //             // Stop it entirely in the air. This guarantees it restarts from 0 when hitting the ground!
    //             PlayerHammerAnim?.Stop();
    //         }
    //         else if (animName == "GroundMove" || animName == "Idle")
    //         {
                
    //             if (PlayerHammerAnim != null && PlayerHammerAnim.HasAnimation("Hammer"))
    //             {
    //                 // If it was just paused in Idle, this safely resumes it.
    //                 PlayerHammerAnim.Play("Hammer");
    //             }

    //             // else if (HasGoldenHammer)
    //             // {
    //             //     if (PlayerHammerAnim != null && PlayerHammerAnim.HasAnimation("GoldenHammer"))
    //             //     {
    //             //         // If it was just paused in Idle, this safely resumes it.
    //             //         PlayerHammerAnim.Play("GoldenHammer");
    //             //     }
    //             // }
                
    //         }

    //         if (PlayerAnim != null && PlayerAnim.HasAnimation(hammerAnim))
    //         {
    //             PlayerAnim.Play(hammerAnim);
                
    //             // If Mario was standing still when he grabbed it, instantly pause the swing loop
    //             if (animName == "Idle") PauseAnimations(); 
                
    //             return;
    //         }
    //     }

    //     if (HasGoldenHammer)
    //     {
    //         string hammerAnim = animName + "Hammer";
            
    //         if (animName == "Jump" || animName == "Fall")
    //         {
    //             // Stop it entirely in the air. This guarantees it restarts from 0 when hitting the ground!
    //             PlayerHammerAnim?.Stop();
    //         }
    //         else if (animName == "GroundMove" || animName == "Idle")
    //         {
    //             if (PlayerHammerAnim != null && PlayerHammerAnim.HasAnimation("GoldenHammer"))
    //             {
    //                 // If it was just paused in Idle, this safely resumes it.
    //                 PlayerHammerAnim.Play("GoldenHammer");
    //             }
    //         }

    //         if (PlayerAnim != null && PlayerAnim.HasAnimation(hammerAnim))
    //         {
    //             PlayerAnim.Play(hammerAnim);
                
    //             // If Mario was standing still when he grabbed it, instantly pause the swing loop
    //             if (animName == "Idle") PauseAnimations(); 
                
    //             return;
    //         }
    //     }

    //     if (PlayerAnim != null && PlayerAnim.HasAnimation(animName))
    //     {
    //         PlayerAnim.Play(animName);
    //     }
    // }

    // NEW: Cleanly pauses both the body and the hammer!
    public void PauseAnimations()
    {
        // We ONLY pause the body animation now so Mario's legs stop moving.
        PlayerAnim?.Pause();
        
        // REMOVED: if (HasHammer) PlayerHammerAnim?.Pause();
        // By removing the hammer pause, PlayerHammerAnim continues running freely. 
        // Mario will keep swinging the hammer infinitely while standing still!
    }


    // ==========================================
    // HAMMER REMOVAL & ANIMATION SYNC
    // ==========================================
    public void RemoveHammer()
    {
        HasHammer = false;
        HasGoldenHammer = false;
        
        // 1. Explicitly ensure the Hammer overlay animation is completely stopped
        PlayerHammerAnim?.Stop();

        // 2. Seamlessly transfer the animation time to the non-hammer counterpart
        if (PlayerAnim != null)
        {
            // THE FIX: Use AssignedAnimation instead of CurrentAnimation!
            // This remembers the animation name even if Mario is currently paused in the Idle state.
            string currentAnim = PlayerAnim.AssignedAnimation; 

            // GD.Print($"Assigned Animation is: {currentAnim}");
            
            // If we are currently playing a Hammer animation (like "GroundMoveHammer")
            if (!string.IsNullOrEmpty(currentAnim) && currentAnim.EndsWith("Hammer"))
            {
                double currentPosition = PlayerAnim.CurrentAnimationPosition;
                bool isPlaying = PlayerAnim.IsPlaying();
                
                // Strip the word "Hammer" to get the base animation (e.g., "GroundMove")
                string baseAnim = currentAnim.Replace("Hammer", "");
                
                if (PlayerAnim.HasAnimation(baseAnim))
                {
                    PlayerAnim.Play(baseAnim);
                    
                    // Fast-forward to the exact decimal frame we were just on
                    PlayerAnim.Seek(currentPosition, true);
                    
                    // If Mario was standing still (paused), make sure the new animation stays paused!
                    if (!isPlaying) PlayerAnim.Pause();
                }
            }
        }
    }

    // ==========================================
    // MANUAL HAMMER CANCELLATION
    // ==========================================
    public void CancelHammer()
    {
        if (!HasHammer && !HasGoldenHammer) return;

        // 1. Instantly revert Mario's animations back to normal and set HasHammer to false
        RemoveHammer();

        // 2. Find and destroy the physical Hammer node attached to the player
        foreach (Node child in GetChildren())
        {
            // Because your Hammer script is named Hammer, we can safely check for it!
            if (child is Hammer hammer)
            {
                hammer.QueueFree();
                break; 
            }
        }

        // 3. Safely tell the Level to stop the music and return to the normal track
        Node current = this;
        while (current != null)
        {
            if (current.HasMethod("StopHammerMusic"))
            {
                current.Call("StopHammerMusic");
                break;
            }
            current = current.GetParent();
        }
    }
}