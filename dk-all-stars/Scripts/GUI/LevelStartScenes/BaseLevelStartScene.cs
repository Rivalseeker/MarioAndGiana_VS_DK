using Godot;
using System;

public partial class BaseLevelStartScene : Control
{
    // Scene Paths
    [Export] public AudioStream _levelIntroMusicTrack; 
    [Export(PropertyHint.File, "*.tscn")] private string _levelPath;





    private void PlayLevelIntroMusic()
    {
        if (_levelIntroMusicTrack != null && MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayTrack(_levelIntroMusicTrack);
        }
    }

    private void OnLevelIntroEnd()
    {
        if (!string.IsNullOrEmpty(_levelPath)) SceneManager.Instance.LoadSceneAsync(_levelPath);
    }
}
