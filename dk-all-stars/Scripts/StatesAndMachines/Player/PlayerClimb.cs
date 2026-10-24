using Godot;

public partial class PlayerClimb : State
{
    private float _lastVDir = 0f; 

    public override void Enter()
    {
        Player.IsClimbing = true;
        Player.Velocity = Vector2.Zero;
        _lastVDir = 0f; 

        // Turn all detectors ON during climbing
        Player.LadderDetector.Enabled = true;
        Player.LedgeDetector.Enabled = true;
        Player.ChestLedgeDetector.Enabled = true;

        // ==========================================
        // THE FIX: Check TargetClimbPlayBackwards instead of raw input!
        // This ensures the 2-pixel drop only happens if a downward
        // climb was actually triggered from the previous state.
        // ==========================================
        if (Player.IsOnFloor() && Player.TargetClimbPlayBackwards)
        {
            // Drop Mario slightly below the ledge so his head passes through
            Player.GlobalPosition = new Vector2(Player.GlobalPosition.X, Player.GlobalPosition.Y + 2.0f);
        }

        // Setup the initial animations from Requirement 2
        if (Player.TargetClimbPlayBackwards)
        {
            Player.PlayerAnim.PlayBackwards(Player.TargetClimbAnim);
            Player.PlayerAnim.Seek(.325, true);
        }
        else
        {
            Player.PlayerAnim.Play(Player.TargetClimbAnim);
        }

        // GD.Print("Climb");
    }

    public override void PhysicsUpdate(double delta)
    {
        if (Player.IsInPit)
        {
            EmitSignal(SignalName.Transitioned, this, "Dead");
            return;
        }
        
        // THE FIX: Stop climbing physics and animation pauses from executing if dead!
        if (Player.IsDead) return;

        float vDir = Input.GetAxis("Up", "Down");
        
        // Player.LadderDetector.ForceRaycastUpdate();
        // Player.LedgeDetector.ForceRaycastUpdate();
        // Player.ChestLedgeDetector.ForceRaycastUpdate();

        // bool isLadderAbove = Player.LadderDetector.IsColliding() && Player.GetRaycastCustomData(Player.LadderDetector, "IsLadder");

        // // ------------------------------------------
        // // REQUIREMENT 4: REACHING THE TOP
        // // ------------------------------------------
        // if (vDir < 0 && !isLadderAbove)
        // {
        //     // Ask the LedgeDetector (at Mario's feet) if there is an actual floor to hoist onto
        //     bool isLedgeAtFeet = Player.LedgeDetector.IsColliding() && Player.GetRaycastCustomData(Player.LedgeDetector, "IsLedge");

        //     if (isLedgeAtFeet)
        //     {
        //         // NORMAL LEDGE: Snap to the floor and exit climb
        //         Player.GlobalPosition = new Vector2(Player.GlobalPosition.X, Player.LedgeDetector.GetCollisionPoint().Y);
        //         EndClimb();
        //         return;
        //     }
        //     else
        //     {
        //         // BROKEN LADDER: No ledge exists to stand on! 
        //         // Stop all upward momentum so they hang at the top rung, but let them go back down.
        //         vDir = 0; 
        //     }
        // }

        // // ------------------------------------------
        // // REQUIREMENT 4: REACHING THE BOTTOM
        // // ------------------------------------------
        // if (vDir > 0)
        // {
        //     if (Player.IsOnFloor())
        //     {
        //         // We hit solid ground while climbing down!
        //         if (Player.LedgeDetector.IsColliding())
        //         {
        //             Player.GlobalPosition = new Vector2(Player.GlobalPosition.X, Player.LedgeDetector.GetCollisionPoint().Y);
        //         }
        //         EndClimb();
        //         return;
        //     }
        //     else if (!isLadderAbove)
        //     {
        //         // MID-AIR BOTTOM: We are climbing down, not touching floor, and our UP raycast left the ladder!
        //         // Stop all downward momentum, but let them go back up.
        //         vDir = 0; 
        //     }
        // }

        if (!Player.LadderDetector.IsColliding() && Player.LedgeDetector.IsColliding())
        {
            var hitObjectDown = Player.LedgeDetector.GetCollider();

            if (hitObjectDown is TileMapLayer hitTileMapDown) 
            {
                Vector2 hitPoint = Player.LedgeDetector.GetCollisionPoint() + (Player.LedgeDetector.TargetPosition.Normalized() * 2.0f);
                Vector2I tileCoordLocalDown = hitTileMapDown.LocalToMap(hitTileMapDown.ToLocal(hitPoint));
                
                Vector2 localCenter = hitTileMapDown.MapToLocal(tileCoordLocalDown);
                float localTopEdgeY = localCenter.Y - (hitTileMapDown.TileSet.TileSize.Y / 2.0f);
                float exactGlobalTopY = hitTileMapDown.ToGlobal(new Vector2(localCenter.X, localTopEdgeY)).Y;

                bool isLedgeAtFeet = Player.GetRaycastCustomData(Player.LedgeDetector, "IsLedge");

                if (isLedgeAtFeet)
                {
                    Player.GlobalPosition = new Vector2(Player.GlobalPosition.X, exactGlobalTopY);
                    Player.Velocity = new Vector2(0, 10f);
                    Player.MoveAndSlide();

                    EndClimb(true);
                    return;
                }
            }
        }

        // Apply Movement
        if (vDir != 0f)
        {
            if (vDir < 0f)
            {
                if (Player.ChestLedgeDetector.IsColliding() && !Player.GetRaycastCustomData(Player.ChestLedgeDetector, "IsLedge"))
                {
                    Player.PlayerAnim.Play("Climb");
                }
                else if (Player.ChestLedgeDetector.IsColliding() && Player.GetRaycastCustomData(Player.ChestLedgeDetector, "IsLedge"))
                {
                    Player.PlayerAnim.Play("ClimbLedge");
                }
            }
            
            if (vDir > 0f)
            {
                if (Player.ChestLedgeDetector.IsColliding() && !Player.GetRaycastCustomData(Player.ChestLedgeDetector, "IsLedge"))
                {
                    Player.PlayerAnim.Play("Climb");
                }
                else if (Player.ChestLedgeDetector.IsColliding() && Player.GetRaycastCustomData(Player.ChestLedgeDetector, "IsLedge"))
                {
                    Player.PlayerAnim.PlayBackwards("ClimbLedge");
                }
            }

            if ((!Player.LadderDetector.IsColliding() && vDir < 0f) || (!Player.LedgeDetector.IsColliding() && vDir > 0f))
            {
                Player.Velocity = new Vector2(0f, 0f);
                Player.PlayerAnim.Pause();
            }
            else
            {
                Player.Velocity = new Vector2(0f, Player.ClimbSpeed * vDir);
            }
        }
        else
        {
            Player.PlayerAnim.Pause();
            Player.Velocity = new Vector2(0f, 0f);
        }

        // ==========================================
        // THE FIX 1: Run MoveAndSlide BEFORE checking 
        // floor status so IsOnFloor() updates instantly!
        // ==========================================
        Player.MoveAndSlide();

        // ==========================================
        // THE FIX 2: Change vDir > 0 to vDir >= 0!
        // Exit climb if on floor and NOT climbing UP.
        // ==========================================
        if (Player.IsOnFloor() && vDir >= 0)
        {
            EndClimb(true); 
            return; 
        }

        // Safely fall off a suspended ladder mid-air
        if (vDir > 0 && !Player.LadderDetector.IsColliding() && !Player.LedgeDetector.IsColliding())
        {
            EndClimb(false); 
            return;
        }
    }

    // private void HandleAnimations(float vDir)
    // {
    //     if (vDir == 0)
    //     {
    //         Player.PlayerAnim?.Pause();
    //         _lastVDir = 0f; 
    //         return;
    //     }

    //     // REQUIREMENT 3: Chest Detector dictates the animation
    //     bool touchingLedge = Player.ChestLedgeDetector.IsColliding() && Player.GetRaycastCustomData(Player.ChestLedgeDetector, "IsLedge");
    //     string targetAnim = touchingLedge ? "ClimbLedge" : "Climb";

    //     if (Player.PlayerAnim != null && Player.PlayerAnim.HasAnimation(targetAnim))
    //     {
    //         bool animChanged = Player.PlayerAnim.CurrentAnimation != targetAnim;
    //         bool dirChanged = _lastVDir != vDir;
    //         bool isPaused = !Player.PlayerAnim.IsPlaying();

    //         if (animChanged || dirChanged || isPaused)
    //         {
    //             if (vDir < 0) // UP = Forwards
    //             {
    //                 Player.PlayerAnim.Play(targetAnim);
    //             }
    //             else // DOWN = Backwards
    //             {
    //                 Player.PlayerAnim.PlayBackwards(targetAnim);
                    
    //                 if (animChanged && targetAnim == "ClimbLedge")
    //                 {
    //                     Player.PlayerAnim.Seek(Player.PlayerAnim.CurrentAnimationLength, true);
    //                 }
    //             }
    //         }
    //     }

    //     _lastVDir = vDir;
    // }

    public override void Exit()
    {
        Player.IsClimbing = false;
    }

    private void EndClimb(bool reachedTop)
    {
        // THE FIX 3: Only flag for the hoist animation if we reached the top of the ladder!
        Player.JustFinishedClimbing = reachedTop; 
        
        Player.LadderDetector.Enabled = false;
        Player.LedgeDetector.Enabled = false;
        Player.ChestLedgeDetector.Enabled = false;
        EmitSignal(SignalName.Transitioned, this, "Idle");
    }
}