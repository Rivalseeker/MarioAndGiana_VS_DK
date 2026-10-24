using System;

[Serializable]
public class GameData
{
    // Audio Settings (Linear 0-100 scale to match your UI)
    public int MasterVolume { get; set; } = 100;
    public int MusicVolume { get; set; } = 100;
    public int SfxVolume { get; set; } = 100;
    public int SpeakerModeIndex { get; set; } = 0;

    // Video Settings
    public int WindowScaleMultiplier { get; set; } = 3; // 3x scale = 1152x648
    public bool Fullscreen { get; set; } = false;
    public bool VSync { get; set; } = true;
}