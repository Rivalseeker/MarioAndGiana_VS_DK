using Godot;

public partial class BarrelWild : EnemyState
{
    private float _passThroughTimer = 0f;

    public override void Enter()
    {
        if (Enemy is Barrel _barrel)
        {
            _barrel.PlayAnimation("Wild");
            _passThroughTimer = 0f;
            
            // Guarantee collision is active when the state begins
            _barrel.SetCollisionActive(true);
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        if (Enemy is not Barrel _barrel) return;

        // 1. Direction Area Check: Break out of the wild state!
        if (_barrel.CurrentDirectionArea != null)
        {
            EmitSignal(SignalName.Transitioned, this, "BarrelBounce");
            return;
        }

        // 2. Pass-Through Timer Logic
        if (_passThroughTimer > 0)
        {
            _passThroughTimer -= (float)delta;
            
            // Re-enable collision once it has safely passed through the tile
            if (_passThroughTimer <= 0)
            {
                _barrel.SetCollisionActive(true);
            }
        }

        // 3. Apply Wild Movement
        Vector2 velocity = _barrel.Velocity;
        
        velocity.X = _barrel.WildHorizontalSpeed * _barrel.WildDirection;
        velocity.Y += _barrel.FallGravity * (float)delta;
        velocity.Y = Mathf.Min(velocity.Y, _barrel.MaxFallSpeed);

        _barrel.Velocity = velocity;

        // 4. Hit the floor: Trigger the bounce and disable collision!
        if (_barrel.IsOnFloor() && _passThroughTimer <= 0)
        {
            _barrel.Velocity = new Vector2(velocity.X, -_barrel.WildBounceVelocity);
            _passThroughTimer = _barrel.WildPassThroughTime;
            
            // Disable collision so it passes through the floor on the way down
            _barrel.SetCollisionActive(false); 
        }
    }

    public override void Exit()
    {
        if (Enemy is Barrel _barrel)
        {
            // Failsafe: Ensure collision is always restored when leaving this state
            _barrel.SetCollisionActive(true);
        }
    }
}