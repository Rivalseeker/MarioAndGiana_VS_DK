using Godot;

public partial class Hammer : Area2D
{
    [ExportCategory("Timers")]
    [Export] public float ActiveTime = 8.0f;
    [Export] public float WarningTime = 2.0f; // When it starts flashing/playing "HammerRunningOut"

    [ExportCategory("Node References")]
    [Export] public Area2D SmashHitbox;
    [Export] public Sprite2D HammerSprite;
    [Export] public AnimationPlayer HammerAnim;

    private BasePlayer _wielder;
    private bool _isEquipped = false;

    // NEW: Manual tracking variables
    private float _currentActiveTime = 0f;
    private bool _isExpiring = false;
    private bool _warningPlayed = false;

    public override void _Ready()
    {
        BodyEntered += OnPickup;
        
        if (SmashHitbox != null)
        {
            SmashHitbox.AreaEntered += OnSmashHitboxAreaEntered;
            // Keep the deadly hitbox disabled until Mario picks it up!
            SmashHitbox.SetDeferred(Area2D.PropertyName.Monitoring, false); 
        }

        if (HammerAnim != null && HammerAnim.HasAnimation("Stationary"))
            HammerAnim.Play("Stationary");
    }

    private void OnPickup(Node2D body)
    {
        if (_isEquipped) return;

        if (body is BasePlayer player && !player.IsDead)
        {
            // ==========================================
            // THE FIX: Check if the player already has a hammer
            // ==========================================
            if (player.HasHammer)
            {
                // Find the currently equipped hammer on the player and reset it
                foreach (Node child in player.GetChildren())
                {
                    if (child is Hammer equippedHammer)
                    {
                        equippedHammer.ResetTimer();
                        break;
                    }
                }
                
                // Destroy this newly touched hammer on the ground
                QueueFree();
                
                // Stop the rest of the pickup code from running
                return; 
            }

            // Normal pickup logic continues for empty-handed players
            _isEquipped = true;
            _wielder = player;
            player.HasHammer = true;

            if (!player.IsOnFloor())
            {
                player.PlayAnimation(player.Velocity.Y > 0 ? "Fall" : "Jump");
            }
            else
            {
                player.PlayAnimation(player.IsIdle ? "Idle" : "GroundMove");
            }

            // Disable the pickup trigger safely
            SetDeferred(Area2D.PropertyName.Monitoring, false);

            // Enable the combat hitbox safely
            if (SmashHitbox != null) SmashHitbox.SetDeferred(Area2D.PropertyName.Monitoring, true);

            CallDeferred(MethodName.ReparentToWielder, player);

            if (HammerAnim != null && HammerAnim.HasAnimation("Active")) HammerAnim.Play("Active");
            
            // Tell the Level to swap the music
            BaseDkLevel level = GetLevel();
            if (level != null) level.StartHammerMusic();

            StartExpiration();
        }
    }

    // ==========================================
    // DEFERRED ACTIONS
    // ==========================================
    private void ReparentToWielder(Node2D player)
    {
        if (GetParent() != null)
        {
            GetParent().RemoveChild(this);
            player.AddChild(this);
            Position = Vector2.Zero; // Reset local pos
        }
    }

    public override void _Process(double delta)
    {
        if (_isEquipped && _wielder != null)
        {
            Marker2D grip = _wielder.GetNodeOrNull<Marker2D>("HammerGrip");
            if (grip != null) 
            {
                Position = new Vector2(grip.Position.X * _wielder.FacingDirection, grip.Position.Y);
                Rotation = grip.Rotation * _wielder.FacingDirection;
            }
            
            if (HammerSprite != null) HammerSprite.FlipH = _wielder.FacingDirection < 0;

            // ==========================================
            // NEW: MANUAL TIMER TRACKING
            // Automatically pauses when BasePlayer is paused!
            // ==========================================
            if (_isExpiring)
            {
                _currentActiveTime -= (float)delta;

                // 1. Check if we crossed the Warning threshold
                if (!_warningPlayed && _currentActiveTime <= WarningTime)
                {
                    _warningPlayed = true;
                    if (HammerAnim != null && HammerAnim.HasAnimation("HammerRunningOut"))
                        HammerAnim.Play("HammerRunningOut");
                }

                // 2. Check if the time has completely run out
                if (_currentActiveTime <= 0)
                {
                    _isExpiring = false;
                    ExpireHammer();
                }
            }
        }
    }

    private void StartExpiration()
    {
        // Setup the manual timer variables
        _currentActiveTime = ActiveTime;
        _isExpiring = true;
        _warningPlayed = false;
    }

    public void ResetTimer()
    {
        // Reset the manual tracking variables back to full
        _currentActiveTime = ActiveTime;
        _warningPlayed = false;
        _isExpiring = true;

        // Force the active animation to replay from the beginning
        if (HammerAnim != null && HammerAnim.HasAnimation("Active"))
        {
            HammerAnim.Play("Active");
        }
    }

    private void ExpireHammer()
    {
        // 4. Expiration Actions
        if (_wielder != null && !_wielder.IsDead)
        {
            _wielder.RemoveHammer();
        }

        BaseDkLevel level = GetLevel();
        if (level != null) level.StopHammerMusic();

        QueueFree();
    }

    // ==========================================
    // COMBAT HIT REGISTRATION
    // ==========================================
    private void OnSmashHitboxAreaEntered(Area2D area)
    {
        if (area.Owner is BaseEnemy enemy) ExecuteSmash(enemy);
    }

    private void ExecuteSmash(Node2D target)
    {
        Callable.From(() => 
        {
            BaseDkLevel level = GetLevel();
            
            if (level != null && IsInstanceValid(target))
            {
                level.TriggerHammerSmash(target, target.GlobalPosition);
            }
        }).CallDeferred();
    }

    // ==========================================
    // UTILITY
    // ==========================================
    private BaseDkLevel GetLevel()
    {
        Node current = this;
        while (current != null)
        {
            if (current is BaseDkLevel level) return level;
            current = current.GetParent();
        }
        return null;
    }
}





// using Godot;

// public partial class Hammer : Area2D
// {
//     [ExportCategory("Timers")]
//     [Export] public float ActiveTime = 8.0f;
//     [Export] public float WarningTime = 2.0f; // When it starts flashing/playing "HammerRunningOut"

//     [ExportCategory("Node References")]
//     [Export] public Area2D SmashHitbox;
//     [Export] public Sprite2D HammerSprite;
//     [Export] public AnimationPlayer HammerAnim;

//     private BasePlayer _wielder;
//     private bool _isEquipped = false;

//     public override void _Ready()
//     {
//         BodyEntered += OnPickup;
        
//         if (SmashHitbox != null)
//         {
//             // SmashHitbox.BodyEntered += OnSmashHitboxEntered;
//             SmashHitbox.AreaEntered += OnSmashHitboxAreaEntered;
//             // Keep the deadly hitbox disabled until Mario picks it up!
//             SmashHitbox.SetDeferred(Area2D.PropertyName.Monitoring, false); 
//         }

//         if (HammerAnim != null && HammerAnim.HasAnimation("Stationary"))
//             HammerAnim.Play("Stationary");
//     }

//     private void OnPickup(Node2D body)
//     {
//         if (_isEquipped) return;

//         if (body is BasePlayer player && !player.IsDead)
//         {
//             _isEquipped = true;
//             _wielder = player;
//             player.HasHammer = true;

//             // THE FIX: Dynamically check the player's physics and state instead of forcing "Idle"!
//             if (!player.IsOnFloor())
//             {
//                 // If in the air, check if moving up or down
//                 player.PlayAnimation(player.Velocity.Y > 0 ? "Fall" : "Jump");
//             }
//             else
//             {
//                 // If on the ground, check the custom IsIdle flag from your PlayerIdle state!
//                 player.PlayAnimation(player.IsIdle ? "Idle" : "GroundMove");
//             }

//             // Disable the pickup trigger safely
//             SetDeferred(Area2D.PropertyName.Monitoring, false);

//             // Enable the combat hitbox safely
//             if (SmashHitbox != null) SmashHitbox.SetDeferred(Area2D.PropertyName.Monitoring, true);

//             CallDeferred(MethodName.ReparentToWielder, player);

//             if (HammerAnim != null && HammerAnim.HasAnimation("Active")) HammerAnim.Play("Active");
            
//             // Tell the Level to swap the music
//             BaseDkLevel level = GetLevel();
//             if (level != null) level.StartHammerMusic();

//             StartExpiration();
//         }
//     }

//     // ==========================================
//     // DEFERRED ACTIONS
//     // ==========================================
//     private void ReparentToWielder(Node2D player)
//     {
//         // This now executes safely AFTER the physics engine has unlocked!
//         if (GetParent() != null)
//         {
//             GetParent().RemoveChild(this);
//             player.AddChild(this);
//             Position = Vector2.Zero; // Reset local pos
//         }
//     }

//     public override void _Process(double delta)
//     {
//         if (_isEquipped && _wielder != null)
//         {
//             Marker2D grip = _wielder.GetNodeOrNull<Marker2D>("HammerGrip");
//             if (grip != null) 
//             {
//                 // 1. Copy the animated position. 
//                 // We multiply the X position by FacingDirection so it swings to the left when flipped!
//                 Position = new Vector2(grip.Position.X * _wielder.FacingDirection, grip.Position.Y);
                
//                 // 2. Copy the animated rotation. 
//                 // If Mario is facing left (-1), a 90-degree swing perfectly converts into a -90-degree swing!
//                 Rotation = grip.Rotation * _wielder.FacingDirection;
//             }
            
//             // 3. Flip the sprite's internal shading/drawing if necessary
//             if (HammerSprite != null) HammerSprite.FlipH = _wielder.FacingDirection < 0;
            
//             // NOTE: We completely removed the manual SmashHitbox positioning code! 
//             // Because we are rotating the entire Hammer node by 90/-90 degrees now, 
//             // the child Hitbox will automatically swing down into the lethal zone for us!
//         }
//     }

//     private async void StartExpiration()
//     {
//         // 1. Wait for the main duration
//         await ToSignal(GetTree().CreateTimer(ActiveTime - WarningTime, false), SceneTreeTimer.SignalName.Timeout);
        
//         // 2. Play the warning animation
//         if (HammerAnim != null && HammerAnim.HasAnimation("HammerRunningOut"))
//             HammerAnim.Play("HammerRunningOut");

//         // 3. Wait for the final warning seconds
//         await ToSignal(GetTree().CreateTimer(WarningTime, false), SceneTreeTimer.SignalName.Timeout);

//         // 4. Expiration Actions
//         if (_wielder != null && !_wielder.IsDead)
//         {
//             _wielder.RemoveHammer();
//         }

//         BaseDkLevel level = GetLevel();
//         if (level != null) level.StopHammerMusic();

//         QueueFree();
//     }

//     // ==========================================
//     // COMBAT HIT REGISTRATION
//     // ==========================================
//     // private void OnSmashHitboxEntered(Node2D body)
//     // {
//     //     // Adjust "BaseEnemy" here if your Fireballs/Barrels inherit from something else!
//     //     if (body is BaseEnemy enemy) ExecuteSmash(enemy);
//     // }

//     private void OnSmashHitboxAreaEntered(Area2D area)
//     {
//         // If the enemy uses an Area2D for its hurtbox instead of a KinematicBody
//         if (area.Owner is BaseEnemy enemy) ExecuteSmash(enemy);
//     }

//     private void ExecuteSmash(Node2D target)
//     {
//         // THE FIX: Wrap the logic in a Callable and defer it so it runs 
//         // AFTER the physics engine has completely finished its current frame!
//         Callable.From(() => 
//         {
//             BaseDkLevel level = GetLevel();
            
//             // Ensure the target wasn't deleted by something else in the split-second delay
//             if (level != null && IsInstanceValid(target))
//             {
//                 level.TriggerHammerSmash(target, target.GlobalPosition);
//             }
//         }).CallDeferred();
//     }

//     // ==========================================
//     // UTILITY
//     // ==========================================
//     private BaseDkLevel GetLevel()
//     {
//         Node current = this;
//         while (current != null)
//         {
//             if (current is BaseDkLevel level) return level;
//             current = current.GetParent();
//         }
//         return null;
//     }
// }