using Godot;
using System;

public partial class Hint8 : LevelHint
{
    [ExportCategory("Dependencies")]
    [Export] private Node _nodeToWatch;




    public override void _Ready()
    {
        if (_nodeToWatch != null)
        {
            _nodeToWatch.TreeExited += OnWatchedNodeDestroyed;
        }

        // Force the hint to be invisible when the level starts
        Hide();

        // Wire up the collision signals
        if (_hintArea != null)
        {
            _hintArea.BodyEntered += OnBodyEntered;
            _hintArea.BodyExited += OnBodyExited;
        }
    }




    private void OnWatchedNodeDestroyed()
    {
        // Destroy this Hint node
        QueueFree();
    }
}
