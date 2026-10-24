using Godot;
using System; 

public partial class BarrelBounce : EnemyState
{
    private Random _random = new Random();

    public override void Enter()
    {
        if (Enemy is Barrel _barrel)
        {
            // NEW: Check if the barrel is inside a direction zone
            if (_barrel.CurrentDirectionArea != null)
            {
                _barrel.RollDirection = _barrel.CurrentDirectionArea.ForcedDirection;
            }
            else
            {
                // Fallback: Pick a random horizontal direction for the bounce
                _barrel.RollDirection = _random.Next(0, 2) == 0 ? 1 : -1;
            }

            // Apply the upward bounce velocity AND the new horizontal speed!
            _barrel.Velocity = new Vector2(_barrel.RollSpeed * _barrel.RollDirection, -_barrel.BounceVelocity);

            _barrel.BarrelAnim.Play("Roll");
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        if (Enemy is not Barrel _barrel) return;

        Vector2 velocity = _barrel.Velocity;

        velocity.Y += _barrel.FallGravity * (float)delta;
        velocity.X = Mathf.MoveToward(velocity.X, 0, _barrel.FallFriction * (float)delta); 

        _barrel.Velocity = velocity;

        if (velocity.Y >= 0)
        {
            EmitSignal(SignalName.Transitioned, this, "BarrelFall");
        }
    }
}