using Godot;
using System;

public partial class GirderPlatform1 : AnimatableBody2D
{
    [Export] private Area2D platformTrigger;
    [Export] private AnimationPlayer platformAnim;

    public override void _Ready()
    {
        // 1. Prevent double-transform conflicts with SyncToPhysics when MovingGirders moves.
        // RemoteTransform2D (inside MovingGirders) will still carry this platform down and across!
        TopLevel = true;

        // 2. Ensure the AnimationPlayer updates on the Physics tick, not Idle (_Process)
        if (platformAnim != null)
        {
            platformAnim.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Physics;
        }

        // 3. Disable built-in TileSet collisions on the visual TileMapLayer child 
        // so it doesn't fight with GirderPlatform1's CollisionShape2D
        TileMapLayer girderTiles = GetNodeOrNull<TileMapLayer>("GirderTiles");
        if (girderTiles != null)
        {
            girderTiles.CollisionEnabled = false;
        }

        if (platformTrigger != null && platformAnim != null)
        {
            platformTrigger.BodyEntered += OnTriggerAreaBodyEntered;
        }
    }

    private void OnTriggerAreaBodyEntered(Node2D body)
    {
        if (body is BasePlayer)
        {
            platformAnim.Play("GirderMove");
        }
    }
}