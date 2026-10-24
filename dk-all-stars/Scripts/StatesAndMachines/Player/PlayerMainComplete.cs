using Godot;
using System;

public partial class PlayerMainComplete : State
{
    public override void Enter()
    {
        // if (Enemy is DonkeyKong dk)
        // {
        //     // Lock his physics and bypass gravity in his _PhysicsProcess
        //     dk.Velocity = Vector2.Zero; 
        //     dk.IsDefeated = true; 
            
        //     // Stop any currently playing internal animations so the level's 
        //     // master cutscene AnimationPlayer has full, unconflicted control.
        //     if (dk.DkAnim != null)
        //     {
        //         dk.DkAnim.Stop();
        //     }
        // }

        // 1. Flip the switch to shut down BasePlayer's gravity calculations
        Player.IsDead = true; 
        
        // 2. Kill any leftover momentum from the fall
        Player.Velocity = Vector2.Zero;

        if (Player.PlayerAnim != null)
        {
            Player.PlayerAnim.Pause();
        }

        // Player._playerSprite.FlipH = true;
    }

    public override void PhysicsUpdate(double delta)
    {
        // Logic intentionally left blank - DK's movement is now controlled 
        // entirely by the MarioComplete AnimationPlayer track.
    }
}
