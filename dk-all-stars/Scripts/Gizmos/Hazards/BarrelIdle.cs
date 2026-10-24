using Godot;

public partial class BarrelIdle : EnemyState
{
    public override void Enter()
    {
        if (Enemy is Barrel barrel)
        {
            barrel.Velocity = Vector2.Zero;
            
            // NEW: Stop the animation player so it stays frozen in DK's hands
            barrel.StopAnimation();
        }
    }

    public override void PhysicsUpdate(double delta) { }
}