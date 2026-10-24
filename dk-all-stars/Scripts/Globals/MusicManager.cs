using Godot;

public partial class MusicManager : Node
{
    public static MusicManager Instance { get; private set; }

    [Export] private AudioStreamPlayer _audioPlayer;

    private Tween _fadeTween;

    public override void _Ready()
    {
        Instance = this;
    }

    /// <summary>
    /// Plays a track. If the track is already playing, it seamlessly continues.
    /// </summary>
    public void PlayTrack(AudioStream newTrack)
    {
        if (newTrack == null) return;

        // CRITICAL: Check if this exact song is already playing.
        // If it is, do nothing. This prevents the music from restarting
        // when moving between two levels that share the same theme.
        if (_audioPlayer.Stream == newTrack && _audioPlayer.Playing)
        {
            return; 
        }

        // If a fade out was happening, kill it and reset volume
        _fadeTween?.Kill();
        _audioPlayer.VolumeDb = 0; // 0 dB is default un-altered volume

        _audioPlayer.Stream = newTrack;
        _audioPlayer.Play();
    }

    /// <summary>
    /// Stops the music instantly.
    /// </summary>
    public void StopTrack()
    {
        _audioPlayer.Stop();
        _audioPlayer.Stream = null;
    }

    /// <summary>
    /// Fades out the currently playing track over a set duration.
    /// </summary>
    public void FadeOut(float duration = 1.0f)
    {
        if (!_audioPlayer.Playing) return;

        // Kill any existing tween to prevent conflicts
        _fadeTween?.Kill();
        _fadeTween = CreateTween();

        // Tween the VolumeDb property from 0 down to -60 (essentially silent in Godot)
        _fadeTween.TweenProperty(_audioPlayer, "volume_db", -60.0f, duration)
                  .SetTrans(Tween.TransitionType.Linear);

        // When the tween finishes, actually stop the player and reset the volume for the next song
        _fadeTween.TweenCallback(Callable.From(() => 
        {
            StopTrack();
            _audioPlayer.VolumeDb = 0;
        }));
    }
}