using Godot;
using System;

public partial class Egg : Area2D
{
    private float fallSpeed = 30f;




    public override void _Ready()
    {
        // NEW: Wire up the Hitbox to listen for Mario!
        if (this != null)
        {
            this.AreaEntered += OnPlayerHurtAreaEntered;
        }
    }


    public override void _PhysicsProcess(double delta)
    {
        var velocity = Vector2.Zero;

        velocity.Y = -fallSpeed;
    }




    private void OnPlayerHurtAreaEntered(Area2D area)
    {
        if (area.Name == "PlayerHurtBox")
        {
            // player && !player.IsDead
            BasePlayer targetPlayer = area.GetParent() as BasePlayer;

            if (!targetPlayer.IsDead)
            {
                targetPlayer.Die();
            }
        }
    }
}
