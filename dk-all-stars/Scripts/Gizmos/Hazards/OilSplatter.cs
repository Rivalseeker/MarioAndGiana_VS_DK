using Godot;

public partial class OilSplatter : Area2D
{
    public Vector2 Velocity;
    public float Gravity = 900.0f; 

    public override void _Ready()
    {
        // Tell the splatter to listen for overlapping Area2Ds
        AreaEntered += OnAreaEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        Velocity.Y += Gravity * (float)delta;
        GlobalPosition += Velocity * (float)delta;

        if (GlobalPosition.Y > 500)
        {
            QueueFree();
        }
    }

    private void OnAreaEntered(Area2D area)
    {
        if (area.Name == "PlayerHurtBox" && area.GetParent() is BasePlayer player)
        {
            player.ApplyOilStain();
            QueueFree(); 
        }
        else if (area.Name == "HurtBox" && area.GetParent() is Fireball fireball)
        {
            // THE FIX: Check if the Fireball is ALREADY oiled. 
            // If it is NOT oiled, apply the effect and destroy the splatter.
            // If it IS oiled, this block is skipped, meaning the splatter survives and passes through!
            if (fireball.StateMachine.CurrentState is not FireballOiled)
            {
                fireball.ReactToOil();
                QueueFree();
            }
        }
    }
}