using Godot;

public partial class OilScreenSplatter : Sprite2D
{
    [ExportCategory("Splatter Settings")]
    [Export] public float Lifespan = 4.0f; // Developer-specified lifespan in seconds

    public override void _Ready()
    {
        // Creates a background timer. The moment it runs out, the splatter deletes itself!
        GetTree().CreateTimer(Lifespan, false).Timeout += QueueFree;
    }
}