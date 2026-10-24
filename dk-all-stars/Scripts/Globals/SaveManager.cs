using Godot;
using System;
using System.Text.Json;

public partial class SaveManager : Node
{
    public static SaveManager Instance { get; private set; }

    // Active Data States
    public GameData CurrentGameData { get; private set; } = new GameData();
    public PlayerData CurrentPlayerData { get; private set; } = new PlayerData();
    
    // Tracking the currently loaded slot (e.g., 1, 2, or 3)
    public int CurrentSlot { get; private set; } = 1;

    // File Paths (user:// maps to the OS AppData/Config folder)
    private const string GameDataPath = "user://game_settings.json";
    private const string PlayerDataPrefix = "user://player_slot_";
    private const string PlayerDataExtension = ".json";

    public override void _Ready()
    {
        Instance = this;
        
        // Always load global game settings as soon as the game launches
        LoadGameData();
        
        // NEW: Tell the engine to actually use the data we just loaded!
        ApplyGameDataOnBoot(); 
    }

    // --- GAME DATA (Settings) ---

    public void SaveGameData()
    {
        string jsonString = JsonSerializer.Serialize(CurrentGameData, new JsonSerializerOptions { WriteIndented = true });
        
        using var file = FileAccess.Open(GameDataPath, FileAccess.ModeFlags.Write);
        file.StoreString(jsonString);
    }

    public void LoadGameData()
    {
        if (FileAccess.FileExists(GameDataPath))
        {
            using var file = FileAccess.Open(GameDataPath, FileAccess.ModeFlags.Read);
            string jsonString = file.GetAsText();
            
            try
            {
                CurrentGameData = JsonSerializer.Deserialize<GameData>(jsonString);
            }
            catch (Exception e)
            {
                GD.PrintErr($"Failed to parse Game Data JSON: {e.Message}");
                CurrentGameData = new GameData(); // Fallback to defaults
            }
        }
        else
        {
            // If no file exists (first time playing), save the defaults immediately
            SaveGameData();
        }
    }

    // --- PLAYER DATA (Progression) ---

    public void SavePlayerData(int slotIndex)
    {
        string filePath = $"{PlayerDataPrefix}{slotIndex}{PlayerDataExtension}";
        string jsonString = JsonSerializer.Serialize(CurrentPlayerData, new JsonSerializerOptions { WriteIndented = true });
        
        using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
        file.StoreString(jsonString);
    }

    public void LoadPlayerData(int slotIndex)
    {
        CurrentSlot = slotIndex;
        string filePath = $"{PlayerDataPrefix}{slotIndex}{PlayerDataExtension}";

        if (FileAccess.FileExists(filePath))
        {
            using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
            string jsonString = file.GetAsText();
            
            try
            {
                CurrentPlayerData = JsonSerializer.Deserialize<PlayerData>(jsonString);
            }
            catch (Exception e)
            {
                GD.PrintErr($"Failed to parse Player Data JSON in slot {slotIndex}: {e.Message}");
                CurrentPlayerData = new PlayerData();
            }
        }
        else
        {
            // If the slot is empty, start fresh
            CurrentPlayerData = new PlayerData();
        }
    }

    public void DeletePlayerData(int slotIndex)
    {
        string filePath = $"{PlayerDataPrefix}{slotIndex}{PlayerDataExtension}";
        if (FileAccess.FileExists(filePath))
        {
            DirAccess.RemoveAbsolute(filePath);
        }
    }

    public bool DoesSaveSlotExist(int slotIndex)
    {
        string filePath = $"{PlayerDataPrefix}{slotIndex}{PlayerDataExtension}";
        return FileAccess.FileExists(filePath);
    }

    private void ApplyGameDataOnBoot()
    {
        // 1. Apply Audio Settings
        ApplyBusVolume("Master", CurrentGameData.MasterVolume);
        ApplyBusVolume("Music", CurrentGameData.MusicVolume);
        ApplyBusVolume("Sound", CurrentGameData.SfxVolume);

        // 2. Apply Video Settings
        Window mainWindow = GetTree().Root;
        if (mainWindow.IsEmbedded()) return; // Abort if running docked in the editor

        DisplayServer.WindowSetVsyncMode(CurrentGameData.VSync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);

        if (CurrentGameData.Fullscreen)
        {
            mainWindow.Mode = Window.ModeEnum.ExclusiveFullscreen;
        }
        else
        {
            mainWindow.Mode = Window.ModeEnum.Windowed;
            
            // Re-calculate the 1x base resolution
            Vector2I baseRes = new Vector2I(384, 216);
            Vector2I newSize = new Vector2I(baseRes.X * CurrentGameData.WindowScaleMultiplier, baseRes.Y * CurrentGameData.WindowScaleMultiplier);
            mainWindow.Size = newSize;

            // Center the window
            Vector2I screenPos = DisplayServer.ScreenGetPosition();
            Vector2I screenSize = DisplayServer.ScreenGetSize();
            Vector2I centerPos = screenPos + (screenSize / 2) - (newSize / 2);
            centerPos.Y = Mathf.Max(0, centerPos.Y);
            mainWindow.Position = centerPos;
        }
    }

    private void ApplyBusVolume(string busName, int volume)
    {
        int busIndex = AudioServer.GetBusIndex(busName);
        
        // If volume is 0, completely mute the bus. Otherwise, calculate the Decibel level.
        AudioServer.SetBusMute(busIndex, volume == 0);
        float dbVolume = Mathf.LinearToDb(volume / 100.0f);
        AudioServer.SetBusVolumeDb(busIndex, dbVolume);
    }
}