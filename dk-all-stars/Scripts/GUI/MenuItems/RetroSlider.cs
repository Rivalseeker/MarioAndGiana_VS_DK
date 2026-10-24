using Godot;

public partial class RetroSlider : HBoxContainer
{
    [ExportCategory("Slider Configuration")]
    [Export] public string BusName = "Master";
    [Export] public string TitleText = "VOLUME";

    [ExportCategory("Node References")]
    [Export] private Label _titleLabel;
    [Export] private Label _valueLabel;
    [Export] private Button _leftArrow;
    [Export] private Button _rightArrow;

    private int _busIndex;

    public int _currentValue = 100;
    // NEW: Public property so the menu can read/write the value
    public int CurrentValue 
    { 
        get => _currentValue; 
        set 
        { 
            _currentValue = Mathf.Clamp(value, 0, 100);
            UpdateDisplay();
            ApplyVolume();
        }
    }
    
    // Variables to handle smooth scrolling (Delayed Auto-Shift)
    private enum HoldDirection { None, Left, Right }
    private HoldDirection _currentHold = HoldDirection.None;

    private double _holdTimer = 0;
    private const double InitialDelay = 0.4; // Time to wait before rapid scrolling starts (seconds)
    private const double RepeatDelay = 0.05; // Time between value ticks during rapid scroll (seconds)

    // Blinking variables
    private double _blinkTimer = 0;
    private bool _arrowsVisible = true;

    public override void _Ready()
    {
        _titleLabel.Text = TitleText;
        _busIndex = AudioServer.GetBusIndex(BusName);

        // Convert current engine dB to our 0-100 scale on startup
        float currentDb = AudioServer.GetBusVolumeDb(_busIndex);
        CurrentValue = Mathf.RoundToInt(Mathf.DbToLinear(currentDb) * 100);
        UpdateDisplay();

        // Hide arrows initially
        _leftArrow.Modulate = Colors.Transparent;
        _rightArrow.Modulate = Colors.Transparent;

        // Wire mouse interactions to focus the row
        _leftArrow.GuiInput += OnArrowGuiInput;
        _rightArrow.GuiInput += OnArrowGuiInput;
    }

    public override void _Process(double delta)
    {
        if (HasFocus())
        {
            HandleBlinking(delta);
            HandleInputLogic(delta);
        }
        else
        {
            // Ensure arrows are hidden when not focused
            _leftArrow.Modulate = Colors.Transparent;
            _rightArrow.Modulate = Colors.Transparent;
        }
    }

    private void HandleBlinking(double delta)
    {
        _blinkTimer += delta;
        if (_blinkTimer >= 0.25) // Blink every 0.25 seconds
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
        bool leftPressed = Input.IsActionPressed("Left") || _leftArrow.IsPressed();
        bool rightPressed = Input.IsActionPressed("Right") || _rightArrow.IsPressed();

        // 1. Detect a brand new press (Initial Click)
        if (leftPressed && _currentHold != HoldDirection.Left)
        {
            _currentHold = HoldDirection.Left;
            ChangeValue(-1);              // Apply first change instantly
            _holdTimer = InitialDelay;    // Start the long pause
        }
        else if (rightPressed && _currentHold != HoldDirection.Right)
        {
            _currentHold = HoldDirection.Right;
            ChangeValue(1);               // Apply first change instantly
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
                if (_currentHold == HoldDirection.Left) ChangeValue(-1);
                if (_currentHold == HoldDirection.Right) ChangeValue(1);

                _holdTimer = RepeatDelay; // Reset timer to the faster repeat rate
            }
        }
    }

    private void ChangeValue(int amount)
    {
        _currentValue = Mathf.Clamp(_currentValue + amount, 0, 100);
        UpdateDisplay();
        ApplyVolume();
    }

    private void UpdateDisplay()
    {
        _valueLabel.Text = _currentValue.ToString();
    }

    private void ApplyVolume()
    {
        // Convert 0-100 linear scale back to Godot's Logarithmic dB scale
        float linearVolume = _currentValue / 100.0f;
        float dbVolume = Mathf.LinearToDb(linearVolume);
        
        // If it's 0, completely mute the bus to avoid any background hiss
        AudioServer.SetBusMute(_busIndex, _currentValue == 0);
        AudioServer.SetBusVolumeDb(_busIndex, dbVolume);
    }

    private void OnArrowGuiInput(InputEvent @event)
    {
        // If the user clicks an arrow, force this entire slider row to grab focus
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            GrabFocus();
        }
    }
}