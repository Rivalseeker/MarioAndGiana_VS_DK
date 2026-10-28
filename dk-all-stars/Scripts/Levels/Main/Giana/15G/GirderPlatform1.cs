using Godot;
using System;

public partial class GirderPlatform1 : AnimatableBody2D
{
    [Export] private Area2D platformTrigger;
    [Export] private AnimationPlayer platformAnim;




    public override void _Ready()
    {
        if (platformTrigger != null && platformAnim != null)
        {
            platformTrigger.BodyEntered += OnTriggerAreaBodyEntered;
        }
    }




    private void OnTriggerAreaBodyEntered(Node2D body)
    {
        if (body is BasePlayer)
        {
            platformAnim.Play("GirderMove");
        }
    }
}
