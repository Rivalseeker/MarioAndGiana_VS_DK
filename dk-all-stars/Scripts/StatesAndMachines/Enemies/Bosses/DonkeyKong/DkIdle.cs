using Godot;

public partial class DkIdle : EnemyState
{
    public override void Enter()
    {
        // Cast the generic Enemy reference into DonkeyKong to access his specific methods
        if (Enemy is DonkeyKong dk)
        {
            // Lock his horizontal momentum so he doesn't slide
            dk.Velocity = new Vector2(0, dk.Velocity.Y); 
            
            // Play the target animation
            dk.PlayAnimation("IdleChestBeat");
            
            // GD.Print("DK Entered Idle State");
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        // He will just stay in this state endlessly for now.
        // In the future, we will add a timer here to transition to a "ThrowBarrel" state!
    }
}