using Godot;
using System;

public partial class HintGroup : Node2D
{
    private void OnPlayerDied()
    {
        Hide();
    }
}
