using Godot;
using System;

public partial class MamaBird : Area2D
{
    [Export] public Sprite2D birdSprite;
    [Export] public AnimationPlayer birdAnim;
    // [Export] public AudioStreamPlayer flySound;
    // [Export] public AudioStreamPlayer swoopSound;




    public override void _Ready()
    {
        // NEW: Wire up the Hitbox to listen for Mario!
        if (this != null && birdSprite != null && birdAnim != null)
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


    private void StateHandler(int stateNumber)
    {
        switch (stateNumber)
        {
            case 0:

                birdAnim.Play("FlyForward");

            break;

            case 1:

                birdAnim.Play("SwoopDown");

            break;

            case 2:

                birdAnim.Play("FlyUp");
            
            break;
        }
        
    }
}
