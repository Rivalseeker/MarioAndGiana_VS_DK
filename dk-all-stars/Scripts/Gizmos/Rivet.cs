using Godot;

public partial class Rivet : StaticBody2D
{
    // NEW: Add a signal that passes a reference to this specific Rivet
    [Signal] public delegate void RivetRemovedEventHandler(Rivet rivet);

    [ExportCategory("Rivet References")]
    [Export] private AnimationPlayer _animPlayer;
    [Export] private Area2D _triggerArea;
    [Export] private CollisionShape2D _rivetCollision; 
    
    // NEW: Export the Tally Label so the level can inject the text into it
    [Export] public Label Tally; 

    private enum EntrySide { None, Left, Right }
    private EntrySide _enteredFrom = EntrySide.None;
    private bool _isTriggered = false;

    public override void _Ready()
    {
        // Wire up both entering and exiting signals
        if (_triggerArea != null)
        {
            _triggerArea.BodyEntered += OnBodyEntered;
            _triggerArea.BodyExited += OnBodyExited;
        }

        if (_animPlayer != null)
        {
            _animPlayer.AnimationFinished += OnAnimationFinished;
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_isTriggered) return;

        if (body is BasePlayer player)
        {
            // Record which side the player entered from based on their relative X position
            if (player.GlobalPosition.X < GlobalPosition.X)
            {
                _enteredFrom = EntrySide.Left;
            }
            else
            {
                _enteredFrom = EntrySide.Right;
            }
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (_isTriggered || _enteredFrom == EntrySide.None) return;

        if (body is BasePlayer player)
        {
            bool crossedSuccessfully = false;

            // Condition 1: Entered from Left, must exit to the Right
            if (_enteredFrom == EntrySide.Left && player.GlobalPosition.X > GlobalPosition.X)
            {
                crossedSuccessfully = true;
            }
            // Condition 2: Entered from Right, must exit to the Left
            else if (_enteredFrom == EntrySide.Right && player.GlobalPosition.X < GlobalPosition.X)
            {
                crossedSuccessfully = true;
            }

            if (crossedSuccessfully)
            {
                _isTriggered = true;
                
                // NEW: Notify the Level exactly before the animation plays!
                EmitSignal(SignalName.RivetRemoved, this);

                _animPlayer?.Play("Removed");

                if (_rivetCollision != null)
                {
                    _rivetCollision.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
                }
            }
            else
            {
                // Player turned around and backed out the same way they came. 
                // Reset the tracking so they can try again.
                _enteredFrom = EntrySide.None;
            }
        }
    }

    private void OnAnimationFinished(StringName animName)
    {
        if (animName == "Removed")
        {
            QueueFree();
        }
    }
}