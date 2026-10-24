using Godot;
using System;

public partial class DonkeyKong : BaseEnemy
{
    public enum DkBehaviorState
    {
        DkIdle,
        DkGrabAndRun,
        DkBarrel,
        DkBarrelAlt, // NEW: Added Alternative Barrel State
        DkMarioDefeat
    }

    [Signal] public delegate void DkStartedEscapeEventHandler();

    [ExportCategory("Node References")]
    [Export] public AnimationPlayer DkAnim;
    [Export] public Area2D Hitbox;
    [Export] public EnemyStateMachine StateMachine; 

    [ExportCategory("Behavior")]
    [Export] public DkBehaviorState StartingState = DkBehaviorState.DkBarrel;

    [ExportCategory("Audio")]
    [Export] public AudioStreamPlayer SfxImpact;
    [Export] public AudioStreamPlayer SfxGrowl;

    [ExportCategory("Escape Sequence")]
    [Export(PropertyHint.Enum, "Left:-1,Right:1")]
    public int EscapeDirection = -1; 
    // [Export] public float EscapeClimbSpeed = 20.0f;
    // NEW: Replaced EscapeClimbSpeed with a teleport distance
    [Export] public float EscapeTeleportDistance = 16.0f;
    [Export] public Vector2 CaptiveCarryOffset = new Vector2(16.0f, -16.0f); 

    [ExportCategory("Barrel Throwing")]
    [Export] public PackedScene BarrelScene;
    [Export] public Marker2D BarrelGrip;
    [Export(PropertyHint.Enum, "Left:-1,Right:1")]
    public int ThrowDirection = 1;
    [Export(PropertyHint.Range, "0.0, 1.0")] 
    public float FlammableBarrelChance = 0.20f; // 20% chance to be flammable
    
    // NEW: Reference to the node where barrels should be stored
    [Export] public Node2D BarrelContainer;

    // NEW: Chance for a standard barrel grab to turn into a wild drop (0.0 to 1.0)
    [Export(PropertyHint.Range, "0.0, 1.0")] 
    public float WildBarrelChance = 0.25f;

    public bool IsEscaping { get; set; } = false;
    public bool IsDefeated { get; set; } = false;
    private Barrel _heldBarrel = null;

    // NEW: Tracking variables
    private bool _isFirstBarrel = true;
    private Random _random = new Random();

    // NEW: Expose the Wild Direction to the Godot Inspector
    [Export(PropertyHint.Enum, "Left:-1,Right:1")]
    public int WildDropDirection = 1;
    

    public override void _Ready()
    {
        if (DkAnim == null) DkAnim = GetNodeOrNull<AnimationPlayer>("DkAnim");
        if (StateMachine == null) StateMachine = GetNodeOrNull<EnemyStateMachine>("StateMachine");

        if (Hitbox != null)
        {
            Hitbox.AreaEntered += OnPlayerHurtAreaEntered;
        }

        if (StateMachine != null)
        {
            StateMachine.CallDeferred(EnemyStateMachine.MethodName.ForceTransition, StartingState.ToString());
        }
    }

    public override void _Process(double delta)
    {
        if (_heldBarrel != null && BarrelGrip != null)
        {
            _heldBarrel.GlobalPosition = BarrelGrip.GlobalPosition;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;
        
        // CHANGED: Added IsDefeated check so gravity and movement are bypassed during the cutscene[cite: 24]
        if (IsEscaping || IsDefeated) 
        {
            // Ensure gravity doesn't pull him down while the AnimationPlayer moves him[cite: 24]
            velocity.Y = 0;
            velocity.X = 0;
        }
        else if (!IsOnFloor())
        {
            velocity.Y += FallGravity * (float)delta;
        }
        
        Velocity = velocity;
        MoveAndSlide();
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

    public void PlayAnimation(string animName)
    {
        if (DkAnim != null && DkAnim.HasAnimation(animName)) DkAnim.Play(animName);
    }

    public void AnimEvent_StartEscapeClimb()
    {
        if (GetNodeOrNull<CollisionShape2D>("CollisionShape2D") is CollisionShape2D colShape)
        {
            colShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        }

        GlobalPosition = new Vector2(GlobalPosition.X + (36.0f * EscapeDirection), GlobalPosition.Y);
        IsEscaping = true;

        EmitSignal(SignalName.DkStartedEscape);
    }

    // ==========================================
    // ESCAPE CLIMBING HOOKS
    // ==========================================

    // NEW: Call this from the AnimationPlayer to snap DK up the ladder!
    public void AnimEvent_TeleportUp()
    {
        if (IsEscaping)
        {
            // Subtracting from Y moves him upwards
            GlobalPosition = new Vector2(GlobalPosition.X, GlobalPosition.Y - EscapeTeleportDistance);
        }
    }

    // ==========================================
    // ANIMATION HOOKS FOR THROWING BARRELS
    // ==========================================

    public void AnimEvent_PlayBarrelGrab()
    {
        // NEW: If we are in the Alt state, flip a coin to decide which grab animation to play!
        if (StateMachine != null && StateMachine.CurrentState.Name == "DkBarrelAlt")
        {
            if (_random.NextDouble() > 0.5)
            {
                PlayAnimation("BarrelGrabAlt");
                return; // Stop here so we don't play the standard animation
            }
        }

        PlayAnimation("BarrelGrab");
    }

    public void AnimEvent_PlayIdle()
    {
        PlayAnimation("Idle");
    }

    public void AnimEvent_SpawnBarrel()
    {
        if (BarrelScene == null || BarrelGrip == null) return;
        
        _heldBarrel = BarrelScene.Instantiate<Barrel>();
        
        // NEW: If it's the first barrel, guarantee it's flammable. Otherwise, roll the dice!
        bool rollFlammable = _isFirstBarrel || _random.NextDouble() <= FlammableBarrelChance;
        
        _heldBarrel.IsFlammable = rollFlammable;
        
        if (BarrelContainer != null)
        {
            BarrelContainer.AddChild(_heldBarrel);
        }
        else
        {
            GetParent().AddChild(_heldBarrel);
        }
        
        _heldBarrel.GlobalPosition = BarrelGrip.GlobalPosition;
        
        if (_heldBarrel.GetNodeOrNull<EnemyStateMachine>("StateMachine") is EnemyStateMachine sm)
        {
            sm.ForceTransition("BarrelIdle");
        }
    }

    // NEW: Method to accept an integer from the AnimationPlayer track
    public void AnimEvent_SetHeldBarrelFrame(int frame)
    {
        if (_heldBarrel != null)
        {
            _heldBarrel.SetFrame(frame);
            // _heldBarrel.SetFlammableFrame(frame);
        }
    }

    public void AnimEvent_SetHeldFlammableBarrelFrame(int frame)
    {
        if (_heldBarrel != null)
        {
            // _heldBarrel.SetFrame(frame);
            _heldBarrel.SetFlammableFrame(frame);
        }
    }

    public void AnimEvent_CheckWildBarrel()
    {
        bool dropWild = false;

        if (_isFirstBarrel)
        {
            dropWild = true;
            _isFirstBarrel = false; 
        }
        else
        {
            dropWild = _random.NextDouble() <= WildBarrelChance;
        }

        if (dropWild)
        {
            if (_heldBarrel != null)
            {
                // THE FIX: Drop the barrel using the exposed editor property instead of hardcoding Right
                _heldBarrel.WildDirection = WildDropDirection;

                if (_heldBarrel.GetNodeOrNull<EnemyStateMachine>("StateMachine") is EnemyStateMachine sm)
                {
                    sm.ForceTransition("BarrelWild");
                }
                
                _heldBarrel = null;
            }
            
            PlayAnimation("Angry");
        }
    }

    public void AnimEvent_ReleaseBarrel()
    {
        if (_heldBarrel != null)
        {
            // Set the direction to DK's specific throw direction
            _heldBarrel.RollDirection = ThrowDirection; 
            
            // IMPORTANT: Tell the BarrelRoll state NOT to randomize this direction!
            _heldBarrel.KeepDirectionOnNextRoll = true; 
            
            if (_heldBarrel.GetNodeOrNull<EnemyStateMachine>("StateMachine") is EnemyStateMachine sm)
            {
                sm.ForceTransition("BarrelRoll");
            }
            
            _heldBarrel = null;
        }
    }

    // NEW: Hook this method up to your "BarrelGrabAlt" animation!
    public void AnimEvent_ReleaseBarrelLeft()
    {
        if (_heldBarrel != null)
        {
            // Force the barrel to roll LEFT (-1)
            _heldBarrel.RollDirection = -1; 
            
            // Tell the BarrelRoll state NOT to randomize this direction!
            _heldBarrel.KeepDirectionOnNextRoll = true; 
            
            if (_heldBarrel.GetNodeOrNull<EnemyStateMachine>("StateMachine") is EnemyStateMachine sm)
            {
                sm.ForceTransition("BarrelRoll");
            }
            
            _heldBarrel = null;
        }
    }
}