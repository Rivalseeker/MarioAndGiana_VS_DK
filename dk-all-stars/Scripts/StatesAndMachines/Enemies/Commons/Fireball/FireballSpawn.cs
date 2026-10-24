using Godot;

public partial class FireballSpawn : EnemyState
{
    [ExportCategory("Kinematic Jump Parameters")]
    [Export] public float JumpHeight = 48.0f; 
    [Export] public float JumpTimeToPeak = 0.4f;
    [Export] public float JumpTimeToDescent = 0.3f; 

    private float _jumpVelocity;
    private float _jumpGravity;
    private float _fallGravity;

    private float _safetyTimer = 0f;
    private const float LaunchGracePeriod = 0.1f; 

    public override void Enter()
    {
        if (Enemy is Fireball fireball)
        {
            // 1. Tell the fireball to stop using default enemy gravity
            fireball.UseCustomGravity = true;

            // 2. Replicate Mario's kinematic formulas exactly
            _jumpVelocity = ((2.0f * JumpHeight) / JumpTimeToPeak) * -1.0f;
            _jumpGravity = (2.0f * JumpHeight) / (JumpTimeToPeak * JumpTimeToPeak);
            _fallGravity = (2.0f * JumpHeight) / (JumpTimeToDescent * JumpTimeToDescent);

            // 3. Combine horizontal speed from the drum with our new precise launch force
            fireball.Velocity = new Vector2(fireball.SpawnLeapVelocity.X, _jumpVelocity);
            
            // 4. Force correct sprite orientation based on movement path
            fireball.FacingDirection = fireball.Velocity.X > 0 ? 1 : -1;
            
            _safetyTimer = LaunchGracePeriod;
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        if (Enemy is not Fireball fireball) return;

        // 1. Manually calculate and apply the correct upward vs downward gravity force
        Vector2 velocity = fireball.Velocity;
        float currentGravity = velocity.Y < 0 ? _jumpGravity : _fallGravity;
        velocity.Y += currentGravity * (float)delta;
        fireball.Velocity = velocity;

        // 2. Process safety window to clear the drum body safely
        if (_safetyTimer > 0)
        {
            _safetyTimer -= (float)delta;
            return; 
        }

        // 3. Check for landfall
        if (fireball.IsOnFloor())
        {
            if (GD.Randf() > 0.5f)
            {
                EmitSignal(SignalName.Transitioned, this, "FireballGroundMove");
            }
            else
            {
                EmitSignal(SignalName.Transitioned, this, "FireballIdle");
            }
        }
    }

    public override void Exit()
    {
        if (Enemy is Fireball fireball)
        {
            // Reset the flag so standard physics behaviors apply normally again
            fireball.UseCustomGravity = false;
        }
    }
}