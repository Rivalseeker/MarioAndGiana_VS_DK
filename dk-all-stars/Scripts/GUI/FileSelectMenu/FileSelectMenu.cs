using Godot;

public partial class FileSelectMenu : Control
{
    [ExportCategory("Menu Navigation")]
    [Export] private Button _firstSlot;

    public override void _Ready()
    {
        // Force the first slot to grab focus so controller/keyboard navigation works immediately
        if (_firstSlot != null)
        {
            _firstSlot.GrabFocus();
        }
    }
}