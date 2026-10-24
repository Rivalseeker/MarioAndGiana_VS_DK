using Godot;

public partial class LevelHint : Node2D
{
    [ExportCategory("Hint Nodes")]
    [Export] public Area2D _hintArea;
    [Export] public AnimationPlayer _animPlayer;
    
    [ExportCategory("Animation Settings")]
    [Export] private string _animationName = "Hint";

    public override void _Ready()
    {
        // Force the hint to be invisible when the level starts
        Hide();

        // Wire up the collision signals
        if (_hintArea != null)
        {
            _hintArea.BodyEntered += OnBodyEntered;
            _hintArea.BodyExited += OnBodyExited;
        }
    }

    public void OnBodyEntered(Node2D body)
    {
        // Only trigger the visual if the exact entering body is the player
        if (body is BasePlayer)
        {
            Show();
            _animPlayer?.Play(_animationName);
        }
    }

    public void OnBodyExited(Node2D body)
    {
        // Hide and stop the animation when the player leaves the zone
        if (body is BasePlayer)
        {
            _animPlayer?.Stop();
            Hide();
        }
    }
}