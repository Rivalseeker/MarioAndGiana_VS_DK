using Godot;
using System;

public partial class PauseScreen : CanvasLayer
{
    [ExportCategory("Prefabs")]
    [Export] private PackedScene _optionsMenuPrefab;

    [ExportCategory("Scene Paths")]
    [Export(PropertyHint.File, "*.tscn")] private string _mainMenuPath;

    [ExportCategory("UI References")]
    [Export] private ColorRect _dimOverlay;
    [Export] private VBoxContainer _menuContainer;

    [ExportCategory("Buttons")]
    [Export] private Button _resumeButton;
    [Export] private Button _checkpointButton;
    [Export] private Button _restartButton;
    [Export] private Button _optionsButton;
    [Export] private Button _exitButton;
    [Export] private Button _closeGameButton;

    [ExportCategory("Cursor Settings")]
    [Export] private TextureRect _cursorIcon;
    [Export] private AnimationPlayer _cursorAnim;
    [Export] private Vector2 _cursorOffset = new Vector2(-24, 0);

    [ExportCategory("Audio")]
    [Export] private AudioStreamPlayer _pauseSfx;




    private bool _isPaused = false;

    public override void _Ready()
    {
        // 1. Wire Button Press Signals (Placeholders)
        _resumeButton.Pressed += UnpauseGame;
        _checkpointButton.Pressed += OnRestartCheckpointPressed;
        _restartButton.Pressed += OnRestartLevelPressed;
        _optionsButton.Pressed += OnOptionsPressed;
        _exitButton.Pressed += OnExitLevelPressed;
        _closeGameButton.Pressed += OnCloseGamePressed;

        // 2. Wire Cursor Tracking to Focus
        _resumeButton.FocusEntered += () => MoveCursorTo(_resumeButton);
        _checkpointButton.FocusEntered += () => MoveCursorTo(_checkpointButton);
        _restartButton.FocusEntered += () => MoveCursorTo(_restartButton);
        _optionsButton.FocusEntered += () => MoveCursorTo(_optionsButton);
        _exitButton.FocusEntered += () => MoveCursorTo(_exitButton);
        _closeGameButton.FocusEntered += () => MoveCursorTo(_closeGameButton);

        // 3. Focus Looping (Wrap-Around Top to Bottom)
        _resumeButton.FocusNeighborTop = _closeGameButton.GetPath();
        _closeGameButton.FocusNeighborBottom = _resumeButton.GetPath();

        // 4. Initial State Setup
        HideMenu();
    }

    public override void _Input(InputEvent @event)
    {
        // Replace "Pause" with the exact string from your Input Map (e.g., "ui_cancel" or "pause")
        if (@event.IsActionPressed("Pause"))
        {
            // Tell the Godot Viewport to stop passing this input down the tree
            GetViewport().SetInputAsHandled(); 
            
            TogglePause();
        }
    }

    private void TogglePause()
    {
        _isPaused = !_isPaused;

        if (_isPaused)
        {
            _pauseSfx?.Play();
            GetTree().Paused = true;
            ShowMenu();
        }
        else
        {
            UnpauseGame();
        }
    }

    private void ShowMenu()
    {
        Visible = true; 
        _dimOverlay.Show();
        _menuContainer.Show();
        _cursorIcon.Show();

        // // Swap to static mud
        // if (_animatedMudLayer != null) _animatedMudLayer.Hide();
        // if (_staticMudLayer != null) _staticMudLayer.Show();

        _resumeButton.CallDeferred(Control.MethodName.GrabFocus);
    }

    private void HideMenu()
    {
        Visible = false;
        _dimOverlay.Hide();
        _menuContainer.Hide();
        _cursorIcon.Hide();

        // // Swap back to animated mud
        // if (_animatedMudLayer != null) _animatedMudLayer.Show();
        // if (_staticMudLayer != null) _staticMudLayer.Hide();
    }

    private void UnpauseGame()
    {
        _isPaused = false;
        GetTree().Paused = false;
        HideMenu();
    }

    // ==========================================
    // CURSOR MOVEMENT LOGIC
    // ==========================================
    private void MoveCursorTo(Control targetControl)
    {
        if (_cursorIcon == null || !Visible) return;

        Vector2 targetPosition = targetControl.GlobalPosition;
        targetPosition.X += _cursorOffset.X;
        targetPosition.Y += (targetControl.Size.Y / 2.0f) - (_cursorIcon.Size.Y / 2.0f) + _cursorOffset.Y;

        _cursorIcon.GlobalPosition = targetPosition.Floor();

        if (_cursorAnim != null)
        {
            _cursorAnim.Stop();
            _cursorAnim.Play("Blink");
        }
    }

    // ==========================================
    // PLACEHOLDER BUTTON ACTIONS
    // ==========================================
    private void OnRestartCheckpointPressed()
    {
        UnpauseGame(); 
        
        // THE FIX: Throw away items collected since the checkpoint
        SessionState.RevertCollectedItems(); 
        
        if (GetParent() is BaseDkLevel level)
        {
            if (MusicManager.Instance != null) MusicManager.Instance.StopTrack();
            level.TransitionToScene(SessionState.CurrentLevelPath);
        }
    }

    private void OnRestartLevelPressed()
    {
        UnpauseGame();
        
        SessionState.ActiveCheckpointId = ""; 
        
        // THE FIX: Total wipe!
        SessionState.ClearAllCollectedItems(); 
        
        if (GetParent() is BaseDkLevel level)
        {
            if (MusicManager.Instance != null) MusicManager.Instance.StopTrack();
            level.TransitionToScene(SessionState.CurrentLevelPath);
        }
    }

    private void OnOptionsPressed()
    {
        if (_optionsMenuPrefab == null)
        {
            GD.PrintErr("Options Menu Prefab is not assigned in the PauseScreen Inspector!");
            return;
        }

        // 1. Tell the SceneManager to spawn and track the overlay
        Node optionsInstance = SceneManager.Instance.OpenMenuOverlay(_optionsMenuPrefab);
        
        if (optionsInstance != null)
        {
            // 2. Disable the PauseScreen so it ignores all UI navigation and inputs
            this.ProcessMode = ProcessModeEnum.Disabled;

            // 3. Listen for the Options Menu closing to wake the PauseScreen back up
            optionsInstance.TreeExited += () => 
            {
                this.ProcessMode = ProcessModeEnum.Always;
                
                // Re-grab focus so the player can immediately resume navigating the pause menu
                _optionsButton.CallDeferred(Control.MethodName.GrabFocus);
            };
        }
    }

    private void OnExitLevelPressed()
    {
        UnpauseGame();
        
        SessionState.ActiveCheckpointId = ""; 
        
        // THE FIX: Total wipe!
        SessionState.ClearAllCollectedItems(); 
        
        if (!string.IsNullOrEmpty(_mainMenuPath))
        {
            if (GetParent() is BaseDkLevel level)
            {
                level.TransitionToScene(_mainMenuPath);
            }
            else
            {
                SceneManager.Instance.LoadSceneAsync(_mainMenuPath);
            }
        }
    }

    private void OnCloseGamePressed()
    {
        GD.Print("Placeholder Action: Quitting to Desktop...");
        GetTree().Quit();
    }
}