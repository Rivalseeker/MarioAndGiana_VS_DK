using Godot;

public partial class PlayerFall : State
{
    private float _startFallY;
    private bool _isLethalFall;
    private bool _fallSoundPlayed;
    
    // NEW: A flag to strictly track if they walked off a ledge
    private bool _walkedOffLedge; 

    public override void Enter()
    {
        // NEW: Instantly revert the visual if the player falls (e.g. walks off a ledge) within the 0.1s window
        Player.RevertLandingVisual();

        if (Player.IsSpinJumping && !Player.HasHammer)
        {
            Player.PlayAnimation("SpinFall");
        }
        else
        {
            Player.PlayAnimation("Fall");
        }
        
        // ... Keep the rest of your Enter() code identical
        _startFallY = Player.GlobalPosition.Y;
        _isLethalFall = false;
        _fallSoundPlayed = false;

        if (Player.CurrentJumps == 0)
        {
            _walkedOffLedge = true; 
            Player.CurrentJumps = 1; 
            Player.Velocity = new Vector2(0, Player.Velocity.Y);
        }
        else
        {
            _walkedOffLedge = false;
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        // 1. Bottomless Pit Check
        if (Player.IsInPit)
        {
            EmitSignal(SignalName.Transitioned, this, "Dead");
            return;
        }

        // 2. Standard Fall Damage Math
        float fallDistance = Player.GlobalPosition.Y - _startFallY;
        
        // THE FIX: Use our new flag instead of checking CurrentJumps!
        float safeDistance = _walkedOffLedge ? 17.0f : (Player.JumpHeight + 17.0f);

        if (fallDistance > safeDistance)
        {
            _isLethalFall = true;
            if (!_fallSoundPlayed)
            {
                Player.PlaySfx("Fall"); 
                _fallSoundPlayed = true;
            }
        }

        // 3. Hit the Ground
        if (Player.IsOnFloor())
        {
            Player.IsSpinJumping = false; 
            
            if (_isLethalFall)
            {
                EmitSignal(SignalName.Transitioned, this, "Dead");
            }
            else
            {
                // NEW: Trigger the visual swap upon hitting the floor safely
                Player.TriggerLandingVisual();

                float direction = Input.GetAxis("Left", "Right");

                if (direction == 0)
                {
                    if (Player.HasHammer)
                    {
                        Player.PlayAnimation("GroundMove");
                        Player.PlayerAnim?.Seek(0.1f, true);
                        Player.PauseAnimations();
                    }
                    else
                    {
                        Player.PlayAnimation("Idle");
                    }
                    EmitSignal(SignalName.Transitioned, this, "Idle"); 
                }
                else
                {
                    EmitSignal(SignalName.Transitioned, this, "GroundMove");
                    
                    if (Player.HasHammer)
                    {
                        Player.PlayerAnim?.Seek(0.1f, true);
                    }
                }
            }
            return;
        }

        // 3. Hit the Ground[cite: 10]
        // if (Player.IsOnFloor())
        // {
        //     Player.IsSpinJumping = false;[cite: 10]
            
        //     if (_isLethalFall)
        //     {
        //         EmitSignal(SignalName.Transitioned, this, "Dead");[cite: 10]
        //     }
        //     else
        //     {
        //         // NEW: Transition directly to the Land state instead of Idle/GroundMove
        //         EmitSignal(SignalName.Transitioned, this, "Land");
        //     }
        //     return;
        // }

        // 4. Double Jump Support
        if (Input.IsActionJustPressed("Jump") && Player.CurrentJumps < Player.MaxJumps)
        {
            if (Player.HasHammer)
            {
                Player.CancelHammer();
            }
            else
            {
                EmitSignal(SignalName.Transitioned, this, "Jump");
                return;
            }
        }
    }
}