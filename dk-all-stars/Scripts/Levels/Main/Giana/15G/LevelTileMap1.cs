using Godot;
using System;

public partial class LevelTileMap1 : TileMapLayer
{
    [ExportCategory("Scroll Settings")]
    [Export] public float ScrollSpeed = 12.0f; // Pixels per second downward
    [Export] public bool StopAtMaxDistance = false;
    [Export] public float MaxScrollDistance = 216.0f;

    private bool _isScrolling = false;
    private float _distanceScrolled = 0.0f;

    // NEW: Expose the scrolling state for BaseDkLevel to read
    public bool IsScrolling => _isScrolling;

    // Call this method from your AnimationPlayer's Call Method Track
    public void StartScrolling()
    {
        _isScrolling = true;
    }

    // Optional: Call this if you want an AnimationPlayer or trigger to halt the scroll
    public void StopScrolling()
    {
        _isScrolling = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_isScrolling) return;

        float step = ScrollSpeed * (float)delta;

        if (StopAtMaxDistance)
        {
            float remaining = MaxScrollDistance - _distanceScrolled;
            if (step >= remaining)
            {
                Position += new Vector2(0, remaining);
                _distanceScrolled = MaxScrollDistance;
                _isScrolling = false;
                return;
            }
            _distanceScrolled += step;
        }

        Position += new Vector2(0, step);
    }
}
