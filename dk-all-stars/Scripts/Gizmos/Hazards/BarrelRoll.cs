using Godot;
using System;

public partial class BarrelRoll : EnemyState
{
    private Random _random = new Random();
    private bool _wasOverLedge = false; // NEW: Prevents dice roll spam

    public override void Enter()
    {
        _wasOverLedge = false; // Reset the flag every time we enter the state

        if (Enemy is Barrel _barrel)
        {
            if (_barrel.KeepDirectionOnNextRoll)
            {
                _barrel.KeepDirectionOnNextRoll = false;
            }
            else
            {
                _barrel.RollDirection = _random.Next(0, 2) == 0 ? 1 : -1;
            }
            
            _barrel.PlayAnimation("Roll");
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        if (Enemy is not Barrel _barrel) return;

        _barrel.Velocity = new Vector2(_barrel.RollSpeed * _barrel.RollDirection, _barrel.Velocity.Y);

        if (_barrel.IsOnWall())
        {
            _barrel.RollDirection *= -1;
        }

        if (_barrel.IsOnFloor())
        {
            // ==========================================
            // LADDER DROP LOGIC
            // ==========================================
            // If moving left (-1), use the Right detector. Otherwise, use the Left.
            RayCast2D activeRay = _barrel.RollDirection == -1 ? _barrel.LedgeDetectorRight : _barrel.LedgeDetectorLeft;
            
            bool isCurrentlyOverLedge = _barrel.IsDetectingLedge(activeRay);

            // Only trigger exactly when the trailing edge FIRST touches the ledge
            if (isCurrentlyOverLedge && !_wasOverLedge)
            {
                if (_random.NextDouble() <= _barrel.LadderDropChance)
                {
                    EmitSignal(SignalName.Transitioned, this, "BarrelLadder");
                    return; // Halt physics updates for this frame!
                }
            }
            
            // Update the tracker for the next frame
            _wasOverLedge = isCurrentlyOverLedge;
        }
        else
        {
            EmitSignal(SignalName.Transitioned, this, "BarrelFall");
        }
    }
}