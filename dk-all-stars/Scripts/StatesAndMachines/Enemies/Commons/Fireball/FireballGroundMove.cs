using Godot;

public partial class FireballGroundMove : EnemyState
{
    private double _timer = 0;

    public override void Enter()
    {
        if (Enemy is Fireball fireball)
        {
            // Pick a random direction every time it enters the state
            fireball.FacingDirection = GD.Randf() > 0.5f ? 1 : -1;
            _timer = GD.RandRange(fireball.MinMoveDuration, fireball.MaxMoveDuration);
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        if (Enemy is not Fireball fireball) return;

        if (!fireball.IsOnFloor())
        {
            EmitSignal(SignalName.Transitioned, this, "FireballFall");
            return;
        }

        _timer -= delta;
        
        if (_timer <= 0)
        {
            if (GD.Randf() > 0.5f)
            {
                EmitSignal(SignalName.Transitioned, this, "FireballIdle");
            }
            else
            {
                EmitSignal(SignalName.Transitioned, this, "FireballGroundMove");
            }
            return;
        }

        // 1. Wall and Edge Avoidance
        if (fireball.IsOnWall() && !fireball.JustSteppedUp)
        {
            fireball.FacingDirection *= -1;
        }
        else if (fireball.FacingDirection == -1 && !fireball.FloorDetectLeft.IsColliding())
        {
            fireball.FacingDirection = 1;
        }
        else if (fireball.FacingDirection == 1 && !fireball.FloorDetectRight.IsColliding())
        {
            fireball.FacingDirection = -1;
        }

        // 2. Ladder & Ledge Detection
        // THE FIX 1: If the climb check returns true, stop running this code immediately!
        if (CheckClimbConditions(fireball))
        {
            return; 
        }

        // 3. Apply Movement (This now ONLY runs if we didn't start climbing!)
        fireball.Velocity = new Vector2(fireball.FacingDirection * fireball.WalkSpeed, fireball.Velocity.Y);
    }

    // THE FIX 2: Dynamically scale climb chances based on Mario's relative Y axis
    private bool CheckClimbConditions(Fireball fireball)
    {
        // Default flat chances for when Mario is dead or on the same Y level
        float chanceUp = 0.05f;
        float chanceDown = 0.05f;

        BasePlayer player = fireball.GetTargetPlayer();
        
        if (player != null && !player.IsDead)
        {
            // Calculate height difference (Godot 2D Y-axis: negative is UP, positive is DOWN)
            float yDiff = player.GlobalPosition.Y - fireball.GlobalPosition.Y;
            
            // A 16px buffer ensures they are considered on the same floor if slightly offset
            if (yDiff < -16.0f) // Mario is safely ABOVE Fireball
            {
                chanceUp = 0.15f;
                chanceDown = 0.01f;
            }
            else if (yDiff > 16.0f) // Mario is safely BELOW Fireball
            {
                chanceUp = 0.01f;
                chanceDown = 0.15f;
            }
        }

        // Determine which RayCasts to check based on facing direction
        RayCast2D upRay = fireball.FacingDirection == -1 ? fireball.LeftLadderDetector : fireball.RightLadderDetector;
        RayCast2D downRay = fireball.FacingDirection == -1 ? fireball.LeftLedgeDetector : fireball.RightLedgeDetector;

        // Check for ladder going UP
        if (fireball.GetRaycastCustomData(upRay, "IsLadder"))
        {
            if (GD.Randf() <= chanceUp)
            {
                StartClimbing(fireball, upRay, -1f); 
                return true;
            }
        }
        // Check for ladder going DOWN
        else if (fireball.GetRaycastCustomData(downRay, "IsLadder"))
        {
            if (GD.Randf() <= chanceDown)
            {
                StartClimbing(fireball, downRay, 1f); 
                return true;
            }
        }
        
        return false; // Did not find a ladder, or failed the random check
    }

    private void StartClimbing(Fireball fireball, RayCast2D activeRay, float direction)
    {
        fireball.CenterOnLadderX(activeRay);
        fireball.CurrentClimbDirection = direction;

        if (direction > 0)
        {
            fireball.GlobalPosition = new Vector2(fireball.GlobalPosition.X, fireball.GlobalPosition.Y + 2.0f);
        }

        EmitSignal(SignalName.Transitioned, this, "FireballClimb");
    }
}






// using Godot;

// public partial class FireballGroundMove : EnemyState
// {
//     private double _timer = 0;

//     public override void Enter()
//     {
//         if (Enemy is Fireball fireball)
//         {
//             // Pick a random direction every time it enters the state
//             fireball.FacingDirection = GD.Randf() > 0.5f ? 1 : -1;
//             _timer = GD.RandRange(fireball.MinMoveDuration, fireball.MaxMoveDuration);
//         }
//     }

//     public override void PhysicsUpdate(double delta)
//     {
//         if (Enemy is not Fireball fireball) return;

//         if (!fireball.IsOnFloor())
//         {
//             EmitSignal(SignalName.Transitioned, this, "FireballFall");
//             return;
//         }

//         _timer -= delta;
        
//         if (_timer <= 0)
//         {
//             if (GD.Randf() > 0.5f)
//             {
//                 EmitSignal(SignalName.Transitioned, this, "FireballIdle");
//             }
//             else
//             {
//                 EmitSignal(SignalName.Transitioned, this, "FireballGroundMove");
//             }
//             return;
//         }

//         // 1. Wall and Edge Avoidance
//         if (fireball.IsOnWall() && !fireball.JustSteppedUp)
//         {
//             fireball.FacingDirection *= -1;
//         }
//         else if (fireball.FacingDirection == -1 && !fireball.FloorDetectLeft.IsColliding())
//         {
//             fireball.FacingDirection = 1;
//         }
//         else if (fireball.FacingDirection == 1 && !fireball.FloorDetectRight.IsColliding())
//         {
//             fireball.FacingDirection = -1;
//         }

//         // 2. Ladder & Ledge Detection
//         // THE FIX 1: If the climb check returns true, stop running this code immediately!
//         if (CheckClimbConditions(fireball))
//         {
//             return; 
//         }

//         // 3. Apply Movement (This now ONLY runs if we didn't start climbing!)
//         fireball.Velocity = new Vector2(fireball.FacingDirection * fireball.WalkSpeed, fireball.Velocity.Y);
//     }

//     // THE FIX 2: Change this from 'void' to 'bool' so it can report back to PhysicsUpdate
//     private bool CheckClimbConditions(Fireball fireball)
//     {
//         if (GD.Randf() > 0.05f) return false; 

//         if (fireball.FacingDirection == -1) 
//         {
//             if (fireball.GetRaycastCustomData(fireball.LeftLadderDetector, "IsLadder"))
//             {
//                 StartClimbing(fireball, fireball.LeftLadderDetector, -1f); 
//                 return true;
//             }
//             else if (fireball.GetRaycastCustomData(fireball.LeftLedgeDetector, "IsLadder"))
//             {
//                 StartClimbing(fireball, fireball.LeftLedgeDetector, 1f); 
//                 return true;
//             }
//         }
//         else 
//         {
//             if (fireball.GetRaycastCustomData(fireball.RightLadderDetector, "IsLadder"))
//             {
//                 StartClimbing(fireball, fireball.RightLadderDetector, -1f); 
//                 return true;
//             }
//             else if (fireball.GetRaycastCustomData(fireball.RightLedgeDetector, "IsLadder"))
//             {
//                 StartClimbing(fireball, fireball.RightLedgeDetector, 1f); 
//                 return true;
//             }
//         }
        
//         return false; // Did not find a ladder
//     }

//     private void StartClimbing(Fireball fireball, RayCast2D activeRay, float direction)
//     {
//         fireball.CenterOnLadderX(activeRay);
//         fireball.CurrentClimbDirection = direction;

//         if (direction > 0)
//         {
//             fireball.GlobalPosition = new Vector2(fireball.GlobalPosition.X, fireball.GlobalPosition.Y + 2.0f);
//         }

//         EmitSignal(SignalName.Transitioned, this, "FireballClimb");
//     }
// }