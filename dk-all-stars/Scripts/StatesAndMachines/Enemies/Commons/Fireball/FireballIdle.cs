using Godot;

public partial class FireballIdle : EnemyState
{
    private double _timer = 0;

    public override void Enter()
    {
        // Cast the generic Enemy into our specific Fireball to access its variables
        if (Enemy is Fireball fireball)
        {
            fireball.Velocity = new Vector2(0, fireball.Velocity.Y);
            
            // THE FIX: Pick a random time between 0.1 and 0.5 seconds!
            _timer = GD.RandRange(fireball.MinIdleDuration, fireball.MaxIdleDuration);

            // GD.Print("Idle");
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        // THE FIX: Transition to Fall if the floor disappears beneath it!
        if (!Enemy.IsOnFloor())
        {
            EmitSignal(SignalName.Transitioned, this, "FireballFall");
            return;
        }

        _timer -= delta;
        if (_timer <= 0 && Enemy.IsOnFloor())
        {
            EmitSignal(SignalName.Transitioned, this, "FireballGroundMove");
        }
    }
}