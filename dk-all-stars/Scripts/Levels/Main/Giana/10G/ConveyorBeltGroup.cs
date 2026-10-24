using Godot;

public partial class ConveyorBeltGroup : Node2D
{
    [Export] public AnimationPlayer MoveAnim;
    [Export] public string AnimName = "ConveyorBeltMove";
    [Export] public double StartOffsetSeconds = 0.5;

    public override void _Ready()
    {
        if (MoveAnim != null && MoveAnim.HasAnimation(AnimName))
        {
            // Start playing the animation sequence
            MoveAnim.Play(AnimName);
            
            // Jump directly to the specified timestamp (true forces an immediate visual/physics update)
            MoveAnim.Seek(StartOffsetSeconds, update: true);
        }
    }
}