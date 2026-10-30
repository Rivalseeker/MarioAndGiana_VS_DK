using Godot;

public partial class PlayerFall : State
{
    private float _startFallY;
    private bool _isLethalFall;
    private bool _fallSoundPlayed;
    
    // A flag to strictly track if they walked off a ledge
    private bool _walkedOffLedge; 

    public override void Enter()
    {
        // Instantly revert the visual if the player falls (e.g. walks off a ledge) within the 0.1s window
        Player.RevertLandingVisual();

        if (Player.IsSpinJumping && (!Player.HasHammer && !Player.HasGoldenHammer))
        {
            Player.PlayAnimation("SpinFall");
        }
        else
        {
            Player.PlayAnimation("Fall");
        }
        
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
                Player.TriggerLandingVisual();

                float direction = Input.GetAxis("Left", "Right");

                if (direction == 0)
                {
                    if (!Player.HasHammer && !Player.HasGoldenHammer)
                    {
                        Player.PlayAnimation("GroundMove");
                        Player.PlayerAnim?.Seek(0.1f, true);
                        Player.PauseAnimations();
                    }
                    else
                    {
                        // Calls BasePlayer.PlayAnimation("Idle"), which now plays 
                        // "GroundMoveHammer", seeks to 0.3s, and pauses the legs!
                        Player.PlayAnimation("Idle");
                    }
                    EmitSignal(SignalName.Transitioned, this, "Idle"); 
                }
                else
                {
                    EmitSignal(SignalName.Transitioned, this, "GroundMove");
                    
                    if (!Player.HasHammer && !Player.HasGoldenHammer)
                    {
                        Player.PlayerAnim?.Seek(0.1f, true);
                    }
                    else
                    {
                        // Start "GroundMoveHammer" at the 0.3s mark when landing into a run
                        Player.PlayerAnim?.Seek(0.3f, true);
                    }
                }
            }
            return;
        }

        // // 3. Hit the Ground
        // if (Player.IsOnFloor())
        // {
        //     Player.IsSpinJumping = false; 
            
        //     if (_isLethalFall)
        //     {
        //         EmitSignal(SignalName.Transitioned, this, "Dead");
        //     }
        //     else
        //     {
        //         Player.TriggerLandingVisual();

        //         float direction = Input.GetAxis("Left", "Right");

        //         if (direction == 0)
        //         {
        //             if (!Player.HasHammer && !Player.HasGoldenHammer)
        //             {
        //                 Player.PlayAnimation("GroundMove");
        //                 Player.PlayerAnim?.Seek(0.1f, true);
        //                 Player.PauseAnimations();
        //             }
        //             else
        //             {
        //                 Player.PlayAnimation("Idle");
        //             }
        //             EmitSignal(SignalName.Transitioned, this, "Idle"); 
        //         }
        //         else
        //         {
        //             EmitSignal(SignalName.Transitioned, this, "GroundMove");
                    
        //             if (!Player.HasHammer && !Player.HasGoldenHammer)
        //             {
        //                 Player.PlayerAnim?.Seek(0.1f, true);
        //             }
        //         }
        //     }
        //     return;
        // }

        // 4. Double Jump Support
        if (Input.IsActionJustPressed("Jump"))
        {
            if (Player.HasHammer || Player.HasGoldenHammer)
            {
                Player.CancelHammer();
            }
            else if (Player.CurrentJumps < Player.MaxJumps)
            {
                EmitSignal(SignalName.Transitioned, this, "Jump");
                return;
            }
        }
    }
}