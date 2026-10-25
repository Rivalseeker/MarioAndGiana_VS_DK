using Godot;
using System;

public partial class MovingGirderStarter : Node
{
    private AnimationPlayer _platform3Anim;
    private AnimationPlayer _platform4Anim;
    private AnimationPlayer _conveyorMoveAnim;




    public override void _Ready()
    {
        _platform3Anim = GetNode<AnimationPlayer>("Platform3Anim");
        _platform4Anim = GetNode<AnimationPlayer>("Platform4Anim");
        _conveyorMoveAnim = GetNode<AnimationPlayer>("ConveyorMoveAnim");

        _platform3Anim.Play("PlatformMove");
        _platform3Anim.Seek(6.35, update: true);

        _platform4Anim.Play("PlatformMove");
        _platform4Anim.Seek(6.35, update: true);

        _conveyorMoveAnim.Play("PlatformMove");
        _conveyorMoveAnim.Seek(6.35, update: true);
    }
}
