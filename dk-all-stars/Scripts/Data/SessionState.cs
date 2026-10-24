using Godot;

public static class SessionState
{
    // Remembers the specific ID of the flag you touched
    public static string ActiveCheckpointId { get; set; } = "";
    
    // Remembers the path to the level currently being played
    public static string CurrentLevelPath { get; set; } = "";

    // Tier 1: Permanent (Saved across deaths)
    public static System.Collections.Generic.List<string> CollectedLevelItems = new System.Collections.Generic.List<string>();
    
    // Tier 2: Temporary (Lost on death if a checkpoint wasn't reached)
    public static System.Collections.Generic.List<string> TempCollectedItems = new System.Collections.Generic.List<string>();

    // ==========================================
    // COLLECTIBLE MEMORY SYSTEM
    // ==========================================
    public static void CommitCollectedItems()
    {
        // Move all temporary items into the permanent bank
        foreach (string item in TempCollectedItems)
        {
            if (!CollectedLevelItems.Contains(item)) CollectedLevelItems.Add(item);
        }
        TempCollectedItems.Clear();
    }

    public static void RevertCollectedItems()
    {
        // Empty the temporary pocket so the items respawn!
        TempCollectedItems.Clear();
    }

    public static void ClearAllCollectedItems()
    {
        // Total wipe for a fresh level start
        CollectedLevelItems.Clear();
        TempCollectedItems.Clear();
    }

    public static string GetCollectibleTallyText(int totalItemsInLevel)
    {
        // The display text counts both permanent AND temporary items
        int currentCount = CollectedLevelItems.Count + TempCollectedItems.Count;
        
        if (currentCount >= totalItemsInLevel)
        {
            return $"Got All!\n{currentCount}/{totalItemsInLevel}";
        }
        
        return $"{currentCount}/{totalItemsInLevel}";
    }
}