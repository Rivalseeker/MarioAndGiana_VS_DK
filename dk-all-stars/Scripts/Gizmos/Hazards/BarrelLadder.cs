using Godot;
using System;

public partial class BarrelLadder : EnemyState
{
    private Random _random = new Random();

    public override void Enter()
    {
        if (Enemy is Barrel _barrel)
        {
            _barrel.PlayAnimation("Wild");

            // NEW: Teleport the barrel to the exact center X of the ladder tile, 
            // and shift it downward to bypass the floor collision
            _barrel.GlobalPosition = new Vector2(_barrel.TargetLedgeX, _barrel.GlobalPosition.Y + 2.0f);
            
            // Nullify horizontal momentum immediately
            _barrel.Velocity = new Vector2(0, _barrel.LadderSpeed);
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        if (Enemy is not Barrel _barrel) return;

        // Force a strict downward path
        _barrel.Velocity = new Vector2(0, _barrel.LadderSpeed);

        if (_barrel.IsOnFloor())
        {
            if (_barrel.CurrentDirectionArea != null)
            {
                _barrel.RollDirection = _barrel.CurrentDirectionArea.ForcedDirection;
            }
            else
            {
                _barrel.RollDirection = _random.Next(0, 2) == 0 ? 1 : -1;
            }

            _barrel.KeepDirectionOnNextRoll = true; 
            EmitSignal(SignalName.Transitioned, this, "BarrelRoll");
        }
    }
}