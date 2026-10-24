using Godot;
using System;

public partial class Spring : Area2D
{
    [Export] public AudioStreamPlayer fallSound;




    public override void _Ready()
    {
        // NEW: Wire up the Hitbox to listen for Mario!
        if (this != null)
        {
            this.AreaEntered += OnPlayerHurtAreaEntered;
        }
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

    private void PlayFallSound()
    {
        fallSound.Play();
    }
}
