using Godot;

public partial class FireballFall : EnemyState
{
    public override void Enter()
    {
        if (Enemy is Fireball fireball)
        {
            // Kill horizontal momentum so it drops straight down like classic DK!
            fireball.Velocity = new Vector2(0, fireball.Velocity.Y);

            // GD.Print("Fall");
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        // Check if we finally hit the ground
        if (Enemy.IsOnFloor())
        {
            EmitSignal(SignalName.Transitioned, this, "FireballIdle");
        }
    }
}