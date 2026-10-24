using Godot;

public partial class BarrelDirectionArea : Area2D
{
    [Export(PropertyHint.Enum, "Left:-1,Right:1")]
    public int ForcedDirection = 1;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    private void OnBodyEntered(Node2D body)
    {
        // If a barrel enters this zone, give it a reference to this area
        if (body is Barrel barrel)
        {
            barrel.CurrentDirectionArea = this;
        }
    }

    private void OnBodyExited(Node2D body)
    {
        // Clear the reference when the barrel completely leaves the zone
        if (body is Barrel barrel && barrel.CurrentDirectionArea == this)
        {
            barrel.CurrentDirectionArea = null;
        }
    }
}