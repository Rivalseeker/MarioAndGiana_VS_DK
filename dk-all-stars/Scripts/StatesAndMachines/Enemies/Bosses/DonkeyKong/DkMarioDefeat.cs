using Godot;

public partial class DkMarioDefeat : EnemyState
{
    public override void Enter()
    {
        if (Enemy is DonkeyKong dk)
        {
            // Lock his physics and bypass gravity in his _PhysicsProcess
            dk.Velocity = Vector2.Zero; 
            dk.IsDefeated = true; 
            
            // Stop any currently playing internal animations so the level's 
            // master cutscene AnimationPlayer has full, unconflicted control.
            if (dk.DkAnim != null)
            {
                dk.DkAnim.Stop();
            }
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        // Logic intentionally left blank - DK's movement is now controlled 
        // entirely by the MarioComplete AnimationPlayer track.
    }
}