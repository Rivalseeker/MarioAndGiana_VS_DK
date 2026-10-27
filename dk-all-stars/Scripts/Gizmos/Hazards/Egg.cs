using Godot;
using System;

public partial class Egg : Area2D
{
    [Export] private float fallSpeed = 40f;
    // [Export] private AudioStreamPlayer dropSound;




    public override void _Ready()
    {
        // NEW: Wire up the Hitbox to listen for Mario!
        if (this != null)
        {
            this.AreaEntered += OnPlayerHurtAreaEntered;
            // dropSound.Play();
        }
    }


    public override void _PhysicsProcess(double delta)
    {
        var velocity = Vector2.Zero;

        velocity.Y = fallSpeed;

        Position += velocity * (float)delta;
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
