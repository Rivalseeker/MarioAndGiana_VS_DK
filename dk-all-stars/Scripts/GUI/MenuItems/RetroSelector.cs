using Godot;
using System;

public partial class RetroSelector : HBoxContainer
{
    [Export] public string TitleText = "SPEAKER";
    [Export] public string[] Options = { "STEREO", "MONO", "SURROUND" };

    [Export] private Label _titleLabel;
    [Export] private Label _valueLabel;
    [Export] private Button _leftArrow;
    [Export] private Button _rightArrow;

    private int _currentIndex = 0;
    
    // Public property for the menu to read/write
    // public int CurrentIndex 
    // { 
    //     get => _currentIndex; 
    //     set 
    //     { 
    //         _currentIndex = Mathf.Wrap(value, 0, Options.Length);
    //         UpdateDisplay();
    //     }
    // }

    public Action<int> OnValueChanged;

    public int CurrentIndex 
    { 
        get => _currentIndex; 
        set 
        { 
            if (_currentIndex != value)
            {
                _currentIndex = Mathf.Wrap(value, 0, Options.Length);
                UpdateDisplay();
                OnValueChanged?.Invoke(_currentIndex); // Fire the event!
            }
        }
    }

    private double _blinkTimer = 0;
    private bool _arrowsVisible = true;

    // Variables to handle smooth scrolling (Delayed Auto-Shift)
    private enum HoldDirection { None, Left, Right }
    private HoldDirection _currentHold = HoldDirection.None;

    private double _holdTimer = 0;
    private const double InitialDelay = 0.4; // Time to wait before rapid scrolling starts
    
    // NOTE: You might want this slightly higher than the slider's 0.05. 
    // Since selectors usually only have 3-10 options, 0.15 prevents it from flying by too fast!
    private const double RepeatDelay = 0.15;




    public override void _Ready()
    {
        _titleLabel.Text = TitleText;
        
        // THE FIX: Give the value label a fixed width so the container doesn't shift when text changes.
        // (You can adjust this 160 number up or down if your retro font needs more space)
        _valueLabel.CustomMinimumSize = new Vector2(160, 0); 
        _valueLabel.HorizontalAlignment = HorizontalAlignment.Center;

        UpdateDisplay();
        
        _leftArrow.Modulate = Colors.Transparent;
        _rightArrow.Modulate = Colors.Transparent;
        
        _leftArrow.GuiInput += (e) => { if (e is InputEventMouseButton m && m.Pressed) GrabFocus(); };
        _rightArrow.GuiInput += (e) => { if (e is InputEventMouseButton m && m.Pressed) GrabFocus(); };
    }

    public override void _Process(double delta)
    {
        if (HasFocus())
        {
            HandleBlinking(delta);
            HandleInputLogic(delta); // THE FIX: Pass delta here!
        }
        else
        {
            _leftArrow.Modulate = Colors.Transparent;
            _rightArrow.Modulate = Colors.Transparent;
        }
    }

    private void HandleBlinking(double delta)
    {
        _blinkTimer += delta;
        if (_blinkTimer >= 0.25)
        {
            _blinkTimer = 0;
            _arrowsVisible = !_arrowsVisible;
            Color blinkColor = _arrowsVisible ? Colors.White : Colors.Transparent;
            _leftArrow.Modulate = blinkColor;
            _rightArrow.Modulate = blinkColor;
        }
    }

    private void HandleInputLogic(double delta)
    {
        // Use IsActionPressed instead of JustPressed to allow holding
        bool leftPressed = Input.IsActionPressed("Left") || _leftArrow.IsPressed();
        bool rightPressed = Input.IsActionPressed("Right") || _rightArrow.IsPressed();

        // 1. Detect a brand new press (Initial Click)
        if (leftPressed && _currentHold != HoldDirection.Left)
        {
            _currentHold = HoldDirection.Left;
            CurrentIndex--;               // Apply first change instantly
            _holdTimer = InitialDelay;    // Start the long pause
        }
        else if (rightPressed && _currentHold != HoldDirection.Right)
        {
            _currentHold = HoldDirection.Right;
            CurrentIndex++;               // Apply first change instantly
            _holdTimer = InitialDelay;    // Start the long pause
        }
        // 2. Detect when the player lets go
        else if (!leftPressed && !rightPressed)
        {
            _currentHold = HoldDirection.None;
        }

        // 3. Process the holding delay
        if (_currentHold != HoldDirection.None)
        {
            _holdTimer -= delta;
            
            // When the timer hits 0, apply the change and reset to the rapid-fire speed
            if (_holdTimer <= 0)
            {
                if (_currentHold == HoldDirection.Left) CurrentIndex--;
                if (_currentHold == HoldDirection.Right) CurrentIndex++;

                _holdTimer = RepeatDelay; // Reset timer to the faster repeat rate
            }
        }
    }

    private void UpdateDisplay()
    {
        if (Options.Length > 0)
        {
            _valueLabel.Text = Options[_currentIndex];
        }
    }

    // NEW: Change the UI without firing the event (prevents infinite loops)
    public void SetValueNoNotify(int index)
    {
        _currentIndex = Mathf.Wrap(index, 0, Options.Length);
        UpdateDisplay();
    }
}