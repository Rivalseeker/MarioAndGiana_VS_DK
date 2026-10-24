using Godot;

public partial class PlayerJump : State
{
    public override void Enter()
    {
        // NEW: Instantly revert the visual if the player jumps within the 0.1s window
        Player.RevertLandingVisual();

        if (Player.CurrentJumps > 0 || !Player.IsOnFloor())
        {
            Player.IsSpinJumping = true;
            Player.PlayAnimation("SpinJump");
        }
        else
        {
            Player.IsSpinJumping = false;
            Player.PlayAnimation("Jump");
        }
        
        Player.PlaySfx("Jump");
        Player.Velocity = new Vector2(Player.Velocity.X, Player.JumpVelocity);
        Player.CurrentJumps++;
    }

    public override void PhysicsUpdate(double delta)
    {
        if (Player.IsInPit)
        {
            EmitSignal(SignalName.Transitioned, this, "Dead");
            return;
        }

        if (Player.Velocity.Y > 0)
        {
            EmitSignal(SignalName.Transitioned, this, "Fall");
            return;
        }

        if (Player.IsOnCeiling())
        {
            Player.Velocity = new Vector2(0, Player.Velocity.Y); 
            EmitSignal(SignalName.Transitioned, this, "Fall");
            return;
        }

        // 3. Premature Landing Safety Catch
        if (Player.IsOnFloor())
        {
            Player.IsSpinJumping = false; 
            
            // NEW: Trigger the visual swap upon hitting the floor
            Player.TriggerLandingVisual();
            
            float direction = Input.GetAxis("Left", "Right");
            
            if (direction == 0)
            {
                Player.PlayAnimation("Idle");
                EmitSignal(SignalName.Transitioned, this, "Idle");
            }
            else
            {
                EmitSignal(SignalName.Transitioned, this, "GroundMove");
            }
            return;
        }
    }
}