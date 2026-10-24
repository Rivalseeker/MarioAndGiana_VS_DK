using Godot;

public partial class PlayerDead : State
{
    public override void Enter()
    {
        // 1. Flip the switch to shut down BasePlayer's gravity calculations
        Player.IsDead = true; 
        
        // 2. Kill any leftover momentum from the fall
        Player.Velocity = Vector2.Zero;

        // THE FIX: If the player has the hammer, destroy it and stop the music!
        if (Player.HasHammer)
        {
            Player.CancelHammer();
        }

        // 3. Start the animation timeline (This safely overrides any animations CancelHammer tried to sync)
        Player.PlayAnimation("Dead");

        // 4. Tell the level to stop the music and prepare the UI prompt
        Player.EmitSignal(BasePlayer.SignalName.PlayerDied);
    }

    public override void PhysicsUpdate(double delta)
    {
        // Do absolutely nothing. BasePlayer is completely frozen in mid-air!
    }
}