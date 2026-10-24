using Godot;
using System;

public partial class CheckpointFlag : Area2D
{
    // Give each flag a unique name in the Inspector (e.g., "Checkpoint1", "Checkpoint2")
    [Export] public string CheckpointId { get; set; } = "";
    [Export] private AnimationPlayer _animPlayer;

    // A C# static event. When one flag is touched, it shouts out to ALL other flags instantly!
    public static event Action<string> CheckpointTriggered;

    private bool _isActive = false;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        CheckpointTriggered += OnGlobalCheckpointTriggered;

        if (_animPlayer != null)
        {
            _animPlayer.AnimationFinished += OnAnimationFinished;
        }
    }

    // Called by the level when it boots up to set the initial visual state
    public void InitializeState(bool makeActive)
    {
        _isActive = makeActive;
        _animPlayer?.Play(makeActive ? "Active" : "Inactive");
    }

    private void OnBodyEntered(Node2D body)
    {
        // If it's already active, ignore the player completely
        if (_isActive) return;

        if (body is BasePlayer)
        {
            _isActive = true;
            SessionState.ActiveCheckpointId = CheckpointId;
            
            // THE NEW LOGIC: Secure any items collected between the start/last checkpoint and now!
            SessionState.CommitCollectedItems();

            _animPlayer?.Play("Activated");

            // Shout out to all other flags in the level to deactivate themselves
            CheckpointTriggered?.Invoke(CheckpointId);
        }
    }

    private void OnGlobalCheckpointTriggered(string newCheckpointId)
    {
        // If another flag was just touched, and WE are currently active, turn ourselves off
        if (newCheckpointId != CheckpointId && _isActive)
        {
            _isActive = false;
            _animPlayer?.Play("Inactive");
        }
    }

    private void OnAnimationFinished(StringName animName)
    {
        if (animName == "Activated" && _isActive)
        {
            _animPlayer?.Play("Active");
        }
    }

    public override void _ExitTree()
    {
        // Always unsubscribe from static events to prevent memory leaks!
        CheckpointTriggered -= OnGlobalCheckpointTriggered;
    }
}