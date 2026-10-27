using Godot;
using System;

public partial class MamaBird : Area2D
{
    [Export] private Sprite2D birdSprite;
    [Export] private AnimationPlayer birdAnim;
    [Export] private Marker2D eggSpawner;
    // [Export] public AudioStreamPlayer flySound;
    // [Export] public AudioStreamPlayer swoopSound;

    [Export] private Node2D eggGroup;

    private Egg newEgg = null;
    [Export] private PackedScene eggScene;




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


    private void DropEgg()
    {
        newEgg = eggScene.Instantiate<Egg>();

        if (eggGroup != null)
        {
            eggGroup.AddChild(newEgg);
            newEgg.GlobalPosition = eggSpawner.GlobalPosition;
        }
    }
}
