using Godot;

public partial class DkGrabAndRun : EnemyState
{
    public override void Enter()
    {
        if (Enemy is DonkeyKong dk)
        {
            // Stop him from falling or sliding
            dk.Velocity = Vector2.Zero; 
            
            // Ensure the escape flag is reset if we restart the level
            dk.IsEscaping = false; 
            
            dk.PlayAnimation("GrabAndRun");
            // GD.Print("DK has grabbed Pauline and is escaping!");
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        // We leave this empty because DK's _PhysicsProcess handles the upward climb 
        // the moment the AnimationPlayer flips the IsEscaping flag to true!
    }
}