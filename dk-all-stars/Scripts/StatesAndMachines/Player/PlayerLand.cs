using Godot;

public partial class PlayerLand : State
{
    private bool _landingFinished = false;

    public override void Enter()
    {
        // Stop all horizontal movement
        Player.Velocity = new Vector2(0, Player.Velocity.Y);
        
        if (Player.HasHammer)
        {
            // Bypass the wrapper to strictly prevent it from appending an extra "Hammer"
            Player.PlayerAnim?.Play("LandHammer");
        }
        else
        {
            Player.PlayAnimation("Land");
        }
        
        _landingFinished = false;

        // Subscribe to the animation finished signal
        Player.LandAnimationFinished += OnLandAnimationFinished;
    }

    public override void Exit()
    {
        // Unsubscribe to prevent memory leaks or ghost calls
        Player.LandAnimationFinished -= OnLandAnimationFinished;
    }

    private void OnLandAnimationFinished()
    {
        _landingFinished = true;
    }

    public override void PhysicsUpdate(double delta)
    {
        if (Player.IsDead) return;

        // 1. Fall Check: If there is no floor while landing, enter Fall state
        if (!Player.IsOnFloor())
        {
            EmitSignal(SignalName.Transitioned, this, "Fall");
            return;
        }

        // 2. Jump Check: Interrupt the landing to jump
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

        // 3. Exit Check: Wait for the Call Method track to set _landingFinished to true
        if (_landingFinished)
        {
            float direction = Input.GetAxis("Left", "Right");
            
            if (Player.HasHammer)
            {
                // Play GroundMove (which resolves to GroundMoveHammer) to ensure swing sync
                Player.PlayAnimation("GroundMove");
                
                if (direction == 0)
                {
                    // If standing still, pause the legs to hold the pose while the hammer swings
                    Player.PauseAnimations();
                    EmitSignal(SignalName.Transitioned, this, "Idle");
                }
                else
                {
                    EmitSignal(SignalName.Transitioned, this, "GroundMove");
                }
            }
            else
            {
                // Standard logic for when the player doesn't have the hammer
                if (direction == 0)
                {
                    Player.PlayAnimation("Idle");
                    EmitSignal(SignalName.Transitioned, this, "Idle");
                }
                else
                {
                    EmitSignal(SignalName.Transitioned, this, "GroundMove");
                }
            }
        }
    }
}