using Godot;
using System;

public partial class Giana15gL1 : BaseDkLevel
{
    // [Export] private TileMapLayer levelEnvironment;




    public override void _PhysicsProcess(double delta)
    {
        if (_activePlayer == null) return;

        // Check pit area overlaps every physics frame
        CheckPitArea();

        if (_activePlayer.IsDead) return;

        CheckLadderStatus();

        if (_playerInGoalZone && (_activePlayer.IsIdle || _activePlayer.IsGroundMove))
        {
            TriggerLevelClear();
        }
    }

    private void CheckPitArea()
    {
        if (_pitArea == null || !_isGameplayActive) return;

        foreach (Node2D body in _pitArea.GetOverlappingBodies())
        {
            if (body is BasePlayer player)
            {
                player.IsInPit = true;
            }
            else if (body is BaseEnemy enemy)
            {
                enemy.QueueFree();
            }
        }
    }
}
