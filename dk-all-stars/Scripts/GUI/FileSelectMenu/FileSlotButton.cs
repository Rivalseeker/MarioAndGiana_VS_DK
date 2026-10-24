using Godot;

public partial class FileSlotButton : Button
{
    [ExportCategory("Focus Colors")]
    [Export] public Color NormalColor1;
    [Export] public Color FocusColor1;
    [Export] public Color NormalColor2;
    [Export] public Color FocusColor2;

    [ExportCategory("UI References")]
    [Export] private Label _titleLabel;
    [Export] private Control _emptyState;
    [Export] private Control _loadedState;
    [Export] private Label _percentageLabel;
    [Export] private TextureRect _marioIcon;
    [Export] private TextureRect _gianaIcon;

    private ShaderMaterial _shaderMaterial;

    public override void _Ready()
    {
        _shaderMaterial = Material as ShaderMaterial;

        FocusEntered += OnFocusEntered;
        FocusExited += OnFocusExited;
        MouseEntered += GrabFocus; 

        OnFocusExited();
    }

    // ==========================================
    // DATA DISPLAY METHODS
    // ==========================================

    public void SetSlotTitle(string title)
    {
        if (_titleLabel != null) _titleLabel.Text = title;
    }

    public void ShowEmptyState()
    {
        if (_emptyState != null) _emptyState.Show();
        if (_loadedState != null) _loadedState.Hide();
    }

    public void ShowLoadedState(float completionPercentage, bool hasMarioUnlocked, bool hasGianaUnlocked)
    {
        if (_emptyState != null) _emptyState.Hide();
        if (_loadedState != null) _loadedState.Show();

        // Format the float so it looks like "45%" or "100%"
        if (_percentageLabel != null) 
            _percentageLabel.Text = $"{Mathf.RoundToInt(completionPercentage)}%";

        // Show/Hide the character icons based on progression
        if (_marioIcon != null) _marioIcon.Visible = hasMarioUnlocked;
        if (_gianaIcon != null) _gianaIcon.Visible = hasGianaUnlocked;
    }

    // ==========================================
    // SHADER FOCUS LOGIC
    // ==========================================

    private void OnFocusEntered()
    {
        if (_shaderMaterial != null)
        {
            _shaderMaterial.SetShaderParameter("color_1_replace", FocusColor1);
            _shaderMaterial.SetShaderParameter("color_2_replace", FocusColor2);
        }
    }

    private void OnFocusExited()
    {
        if (_shaderMaterial != null)
        {
            _shaderMaterial.SetShaderParameter("color_1_replace", NormalColor1);
            _shaderMaterial.SetShaderParameter("color_2_replace", NormalColor2);
        }
    }
}