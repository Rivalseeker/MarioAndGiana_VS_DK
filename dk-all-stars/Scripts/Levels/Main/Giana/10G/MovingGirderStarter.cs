using Godot;
using System;

public partial class MovingGirderStarter : Node
{
    [Export] private AnimationPlayer _platform3Anim;
    [Export] private AnimationPlayer _platform4Anim;
    [Export] private AnimationPlayer _conveyorMoveAnim;




    public override void _Ready()
    {
        // _platform3Anim = GetNode<AnimationPlayer>("Platform3Anim");
        // _platform4Anim = GetNode<AnimationPlayer>("Platform4Anim");
        // _conveyorMoveAnim = GetNode<AnimationPlayer>("ConveyorMoveAnim");

        // _platform3Anim.Seek(4.0, update: true);
        // _platform4Anim.Seek(4.0, update: true);
        // _conveyorMoveAnim.Seek(7, update: true);

        _platform3Anim.Seek(6.4, update: true);
        _platform4Anim.Seek(6.4, update: true);
        _conveyorMoveAnim.Seek(6.4, update: true);
    }



    private void StartPlatforms()
    {
        _platform3Anim.Play("PlatformMove");
        _platform3Anim.Seek(6.4, update: true);
        

        _platform4Anim.Play("PlatformMove");
        _platform4Anim.Seek(6.4, update: true);
        

        _conveyorMoveAnim.Play("ConveyorBeltMove");
        _conveyorMoveAnim.Seek(6.4, update: true);
        

        this.QueueFree();
    }
}
