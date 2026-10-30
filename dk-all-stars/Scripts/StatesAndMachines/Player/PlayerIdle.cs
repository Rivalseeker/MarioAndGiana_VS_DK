using Godot;

public partial class PlayerIdle : State
{
    public override void Enter()
    {
        // 1. Tell the level we are safely standing
        Player.IsIdle = true; 
        
        Player.Velocity = new Vector2(0, Player.Velocity.Y);

        if (Player.IsOnFloor())
        {
            Player.CurrentJumps = 0;
        }

        // THE FIX: Intercept the entry to play the ClimbEnd animation!
        if (Player.JustFinishedClimbing)
        {
            Player.PlayAnimation("ClimbEnd");
            
            // Reset the flag instantly so normal Idling works next time
            Player.JustFinishedClimbing = false; 
        }
        else
        {
            // You can optionally put your standard Player.PlayAnimation("Idle"); here
            // if you have a default breathing/standing animation.
        }

        // GD.Print(Player.PlayerAnim.CurrentAnimation);

        // GD.Print("Idle");
    }

    public override void Exit()
    {
        // Turn the flag off when we start moving/jumping again
        Player.IsIdle = false; 
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
                GD.Print("CANCEL");
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
        if (direction != 0)
        {
            EmitSignal(SignalName.Transitioned, this, "GroundMove");
        }
    }
}