using Godot;

public partial class TitleScreen : Control
{
    // Updated to point to the Main Menu
    [Export(PropertyHint.File, "*.tscn")] private string _mainMenuPath; 

    public override void _Input(InputEvent @event)
    {
        // Check if the event is a key press or a joypad (controller) button press
        bool isAnyKeyPressed = @event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo;
        bool isAnyButtonPressed = @event is InputEventJoypadButton joypadEvent && joypadEvent.Pressed;

        if (isAnyKeyPressed || isAnyButtonPressed)
        {
            SetProcessInput(false); 
            
            if (!string.IsNullOrEmpty(_mainMenuPath))
            {
                SceneManager.Instance.LoadSceneAsync(_mainMenuPath);
            }
        }
    }
}