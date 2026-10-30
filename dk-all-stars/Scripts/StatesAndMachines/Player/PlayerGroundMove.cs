using Godot;

public partial class PlayerGroundMove : State
{
    public override void Enter()
    {
        // GD.Print("GROUNDMOVE");
        Player.PlayAnimation("GroundMove");

        // THE FIX: Reset the jump counter if we transitioned here from falling!
        if (Player.IsOnFloor())
        {
            Player.CurrentJumps = 0;
        }

        // GD.Print("GroundMove");
        Player.IsGroundMove = true;
    }

    public override void Exit()
    {
        // Turn the GroundMove flag off
        Player.IsGroundMove = false; 
    }

    public override void PhysicsUpdate(double delta)
    {
        if (Player.IsInPit)
        {
            EmitSignal(SignalName.Transitioned, this, "Dead");
            return;
        }

        if (Player.IsDead) return;

        // 1. DYNAMIC RAYCAST TOGGLING
        // THE FIX: Prevent raycasts from turning on if the player has the hammer!
        Player.LadderDetector.Enabled = Input.IsActionPressed("Up") && (!Player.HasHammer && !Player.HasGoldenHammer);
        Player.LedgeDetector.Enabled = Input.IsActionPressed("Down") && (!Player.HasHammer && !Player.HasGoldenHammer);
        Player.ChestLedgeDetector.Enabled = false;

        // Force an immediate physics update on the active rays
        if (Player.LadderDetector.Enabled) Player.LadderDetector.ForceRaycastUpdate();
        if (Player.LedgeDetector.Enabled) Player.LedgeDetector.ForceRaycastUpdate();

        // 2. ENTRY CHECKS (Requirement 2)
        if (Player.LadderDetector.Enabled && Player.LadderDetector.IsColliding())
        {
            if (Player.GetRaycastCustomData(Player.LadderDetector, "IsLadder"))
            {
                Player.CenterOnLadderX(Player.LadderDetector);
                Player.TargetClimbAnim = "Climb";
                Player.TargetClimbPlayBackwards = false;
                EmitSignal(SignalName.Transitioned, this, "Climb");
                return;
            }
        }

        if (Player.LedgeDetector.Enabled && Player.LedgeDetector.IsColliding())
        {
            if (Player.GetRaycastCustomData(Player.LedgeDetector, "IsLadder") && 
                Player.GetRaycastCustomData(Player.LedgeDetector, "IsLedge"))
            {
                Player.CenterOnLadderX(Player.LedgeDetector);
                Player.TargetClimbAnim = "ClimbLedge";
                Player.TargetClimbPlayBackwards = true; // Start from end to climb down
                // GD.Print("CLIMBLEDGE");
                EmitSignal(SignalName.Transitioned, this, "Climb");
                return;
            }
        }

        // 2. Standard Physics
        if (!Player.IsOnFloor())
        {
            EmitSignal(SignalName.Transitioned, this, "Fall");
            // Player.CurrentJumps++;
            return;
        }

        // THE FIX: Intercept the Jump button to drop the hammer!
        if (Input.IsActionJustPressed("Jump"))
        {
            if (Player.HasHammer || Player.HasGoldenHammer)
            {
                // Cancel the hammer! (Because it doesn't call EmitSignal, they won't jump)
                Player.CancelHammer();
            }
            else if (Player.CurrentJumps < Player.MaxJumps)
            {
                // Normal Jump
                EmitSignal(SignalName.Transitioned, this, "Jump");
                return;
            }
        }

        float direction = Input.GetAxis("Left", "Right");

        if (direction == 0)
        {
            // THE FIX: Use the new method to pause both Animation Players!
            Player.PauseAnimations(); 
            EmitSignal(SignalName.Transitioned, this, "Idle");
            return;
        }

        Player.UpdateFacingDirection(direction);
        Player.Velocity = new Vector2(direction * Player.WalkSpeed, Player.Velocity.Y);
    }
}