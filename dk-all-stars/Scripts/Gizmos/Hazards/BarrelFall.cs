using Godot;

public partial class BarrelFall : EnemyState
{
    private double _fallTimer = 0;

    public override void Enter()
    {
        _fallTimer = 0;
    }

    public override void PhysicsUpdate(double delta)
    {
        if (Enemy is not Barrel _barrel) return;

        _fallTimer += delta;

        Vector2 velocity = _barrel.Velocity;

        velocity.Y += _barrel.FallGravity * (float)delta;
        velocity.Y = Mathf.Min(velocity.Y, _barrel.MaxFallSpeed);
        velocity.X = Mathf.MoveToward(velocity.X, 0, _barrel.FallFriction * (float)delta); 

        _barrel.Velocity = velocity;

        if (_barrel.IsOnFloor())
        {
            if (_fallTimer >= _barrel.MinimumFallTimeToBounce)
            {
                EmitSignal(SignalName.Transitioned, this, "BarrelBounce");
            }
            else
            {
                // Reverted back to its original state
                _barrel.KeepDirectionOnNextRoll = true; 
                EmitSignal(SignalName.Transitioned, this, "BarrelRoll");
            }
        }
    }
}