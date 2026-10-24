using Godot;
using System;

public partial class Steamroller : AnimatableBody2D
{
    [ExportCategory("Movement Settings")]
    [Export] public float RollerSpeed = 24.0f;
    // [Export] public bool IsMovingRight = true;

    [ExportCategory("Node References")]
    [Export] private AnimatableBody2D rollerBody;
    [Export] private AnimationPlayer rollerAnim;




    public override void _Ready()
    {
        // ApplyVelocityAndVisuals(IsMovingRight);
    }




    private void ApplyVelocityAndVisuals(bool movingRight)
    {
        // IsMovingRight = movingRight;

        float direction = movingRight ? 1.0f : -1.0f;

        rollerBody.ConstantLinearVelocity = new Vector2(RollerSpeed * direction, 0);

        if (movingRight)
        {
            rollerAnim.Play("SteamrollerMove");
        }
        else
        {
            rollerAnim.PlayBackwards("SteamrollerMove");
        }
    }

    // private void ChangeDirection(bool )
}
