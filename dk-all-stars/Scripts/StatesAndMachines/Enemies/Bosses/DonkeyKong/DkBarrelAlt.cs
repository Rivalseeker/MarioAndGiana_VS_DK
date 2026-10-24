using Godot;

public partial class DkBarrelAlt : EnemyState
{
    public override void Enter()
    {
        if (Enemy is DonkeyKong dk)
        {
            dk.Velocity = new Vector2(0, dk.Velocity.Y); 
            
            // Start the loop with Idle. 
            dk.PlayAnimation("Idle");
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        // Logic intentionally left blank - controlled by DK's AnimationPlayer
    }
}