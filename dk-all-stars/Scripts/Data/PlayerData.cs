using System;
using System.Collections.Generic;

[Serializable]
public class PlayerData
{
    // Progress Tracking
    public int HighestLevelUnlocked { get; set; } = 1;
    public int CurrentLives { get; set; } = 3;
    public int TotalScore { get; set; } = 0;

    // Input Bindings (Maps an action string like "Left" to a physical keycode)
    public Dictionary<string, int> CustomInputs { get; set; } = new Dictionary<string, int>();
}