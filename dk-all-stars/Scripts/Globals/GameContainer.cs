using Godot;
using System;

public partial class GameContainer : Node
{
    [Export] private SubViewportContainer _viewportContainer;
    [Export] private SubViewport _subViewport;
    
    [Export] private Node2D _world2D;
    [Export] private CanvasLayer _guiLayer;
    [Export] private CanvasLayer _loadingScreenLayer;
    [Export] private CanvasLayer _debugLayer;

    [Export] private PackedScene _loadingScreenPrefab; 

    private readonly Vector2I _baseResolution = new Vector2I(384, 216);

    public override async void _Ready()
    {
        // Inject all references into the global SceneManager
        SceneManager.Instance.WorldContainer = _world2D;
        SceneManager.Instance.GuiContainer = _guiLayer;
        SceneManager.Instance.LoadingScreenLayer = _loadingScreenLayer;
        SceneManager.Instance.LoadingScreenPrefab = _loadingScreenPrefab;

        GetTree().Root.SizeChanged += OnWindowSizeChanged;
        
        // Wait 2 process frames before calculating layout to allow 
        // the OS Window Manager to finish any physical resizing from the SaveManager
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        OnWindowSizeChanged();
    }

    private void OnWindowSizeChanged()
    {
        // 1. THE FIX: Lock the scale to 1,1 to prevent the Double-Scale bug!
        _viewportContainer.Scale = Vector2.One;

        // 2. Get the internal rendering resolution (NOT the physical OS window size).
        // This flawlessly respects whatever Stretch Mode your Project Settings are using.
        Vector2 currentViewportSize = GetViewport().GetVisibleRect().Size;

        // 3. Center the viewport container in the middle of the active rendering space
        Vector2 centerMargin = (currentViewportSize - (Vector2)_baseResolution) / 2.0f;
        
        // Round to avoid sub-pixel positioning tearing
        _viewportContainer.Position = centerMargin.Floor(); 
    }
    
    public override void _ExitTree()
    {
        GetTree().Root.SizeChanged -= OnWindowSizeChanged;
    }
}