using Godot;

public partial class SplashScreen : Control
{
    // Updated to use a file path string instead of a PackedScene
    [Export(PropertyHint.File, "*.tscn")] private string _titleScreenPath; 
    
    // private Timer _timer;

    public override void _Ready()
    {
        // _timer = GetNode<Timer>("Timer");
        // _timer.Timeout += OnSplashTimeout;
    }

    // NEW: Listen for hardware input to skip the animation
    public override void _Input(InputEvent @event)
    {
        // Check if the exact action was just pressed this frame
        if (@event.IsActionPressed("Pause"))
        {
            // Consume the input so it doesn't accidentally trigger anything on the next screen
            AcceptEvent(); 
            
            // Instantly trigger the scene load
            OnSplashTimeout(); 
        }
    }

    private void OnSplashTimeout()
    {
        // Tell the global manager to load the title screen asynchronously
        if (!string.IsNullOrEmpty(_titleScreenPath))
        {
            SceneManager.Instance.LoadSceneAsync(_titleScreenPath);
        }
    }
}