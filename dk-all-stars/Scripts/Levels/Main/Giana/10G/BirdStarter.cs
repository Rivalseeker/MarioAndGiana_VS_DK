using Godot;
using System;

public partial class BirdStarter : AnimationPlayer
{
    // [Export] private AnimationPlayer birdPathAnim1;
    // [Export] private AnimationPlayer birdPathAnim2;
    // [Export] private AnimationPlayer birdPathAnim3;
    // [Export] private AnimationPlayer birdPathAnim4;




    private void Delete()
    {
        QueueFree();
    }
}
