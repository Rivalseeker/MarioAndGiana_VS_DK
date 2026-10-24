using Godot;

public partial class FireballClimb : EnemyState
{
    public override void Enter()
    {
        if (Enemy is Fireball fireball)
        {
            fireball.IsClimbing = true;
            fireball.Velocity = Vector2.Zero;

            // GD.Print("Climb");

            // GD.Print(fireball.GlobalPosition.X);
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        if (Enemy is not Fireball fireball) return;

        // GD.Print(fireball.GlobalPosition);

        float vDir = fireball.CurrentClimbDirection;
        
        fireball.ClimbLadderDetector.ForceRaycastUpdate();
        fireball.ClimbLedgeDetector.ForceRaycastUpdate();

        bool isLadderAbove = fireball.GetRaycastCustomData(fireball.ClimbLadderDetector, "IsLadder");
        bool isLedgeAtFeet = fireball.GetRaycastCustomData(fireball.ClimbLedgeDetector, "IsLedge");

        // THE FIX: If there is no ladder, no ledge, and we are suspended in the air, FALL!
        if (!isLadderAbove && !isLedgeAtFeet && !fireball.IsOnFloor())
        {
            EmitSignal(SignalName.Transitioned, this, "FireballFall");
            return;
        }

        // Reaching the top of a valid ladder
        if (vDir < 0 && !isLadderAbove)
        {
            if (isLedgeAtFeet) { SnapToFloorAndExit(fireball); return; }
        }

        // Reaching the bottom of a valid ladder
        if (vDir > 0 && fireball.IsOnFloor())
        {
            SnapToFloorAndExit(fireball);
            return;
        }

        fireball.Velocity = new Vector2(0, vDir * fireball.ClimbSpeed);
    }

    private void SnapToFloorAndExit(Fireball fireball)
    {
        if (fireball.ClimbLedgeDetector.IsColliding() && fireball.ClimbLedgeDetector.GetCollider() is TileMapLayer hitMap)
        {
            Vector2 hitPoint = fireball.ClimbLedgeDetector.GetCollisionPoint() + (fireball.ClimbLedgeDetector.TargetPosition.Normalized() * 2.0f);
            Vector2I cell = hitMap.LocalToMap(hitMap.ToLocal(hitPoint));
            Vector2 localCenter = hitMap.MapToLocal(cell);
            float localTopEdgeY = localCenter.Y - (hitMap.TileSet.TileSize.Y / 2.0f);
            float exactGlobalTopY = hitMap.ToGlobal(new Vector2(localCenter.X, localTopEdgeY)).Y;
            
            fireball.GlobalPosition = new Vector2(fireball.GlobalPosition.X, exactGlobalTopY - 1f);
        }

        // 1. Always prep a random direction just in case GroundMove is chosen
        fireball.FacingDirection = GD.Randf() > 0.5f ? 1 : -1;

        // 2. Randomly choose the next state
        if (GD.Randf() > 0.5f)
        {
            EmitSignal(SignalName.Transitioned, this, "FireballGroundMove");
        }
        else
        {
            EmitSignal(SignalName.Transitioned, this, "FireballIdle");
        }
    }

    public override void Exit()
    {
        if (Enemy is Fireball fireball) fireball.IsClimbing = false;
    }
}