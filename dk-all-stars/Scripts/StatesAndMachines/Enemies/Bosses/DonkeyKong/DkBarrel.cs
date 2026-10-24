using Godot;

public partial class DkBarrel : EnemyState
{
    public override void Enter()
    {
        if (Enemy is DonkeyKong dk)
        {
            dk.Velocity = new Vector2(0, dk.Velocity.Y); 
            
            // Start the loop with Idle. 
            // The animation player's Call Method tracks will handle the rest!
            dk.PlayAnimation("Idle");
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        // Logic intentionally left blank - controlled by DK's AnimationPlayer
    }
}