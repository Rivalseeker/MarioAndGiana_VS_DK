using Godot;

public partial class SceneManager : Node
{
    public static SceneManager Instance { get; private set; }

    // Containers injected by GameContainer
    public Node2D WorldContainer { get; set; }
    public Node GuiContainer { get; set; }
    public Node LoadingScreenLayer { get; set; } 
    private Node _activeOverlay;

    // The prefab of the loading screen UI
    public PackedScene LoadingScreenPrefab { get; set; }

    private Node _currentWorldScene;
    private Node _currentGuiScene;
    private Node _activeLoadingScreen;

    private string _targetScenePath;
    private bool _isLoading = false;

    public override void _EnterTree()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;
        
        // Disable process by default to save performance. We only turn it on when loading.
        SetProcess(false); 
    }

    // Call this instead of ChangeScene() when you want a loading screen
    public void LoadSceneAsync(string scenePath)
    {
        if (_isLoading) return;

        _isLoading = true;
        _targetScenePath = scenePath;

        // 1. Instantiate and display the Loading Screen
        if (LoadingScreenPrefab != null && LoadingScreenLayer != null)
        {
            _activeLoadingScreen = LoadingScreenPrefab.Instantiate();
            LoadingScreenLayer.AddChild(_activeLoadingScreen);
        }

        // 2. Request Godot to load the scene on a background thread
        Error err = ResourceLoader.LoadThreadedRequest(_targetScenePath);
        if (err != Error.Ok)
        {
            GD.PrintErr($"SceneManager: Failed to start loading {_targetScenePath}");
            FinishLoading();
            return;
        }

        // 3. Turn on _Process to monitor the loading status every frame
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        if (!_isLoading) return;

        // Godot requires a specific Array type to fetch the loading percentage
        Godot.Collections.Array progress = new Godot.Collections.Array();
        ResourceLoader.ThreadLoadStatus status = ResourceLoader.LoadThreadedGetStatus(_targetScenePath, progress);

        if (status == ResourceLoader.ThreadLoadStatus.Loaded)
        {
            // The background thread is finished! Retrieve the loaded scene
            PackedScene loadedScene = (PackedScene)ResourceLoader.LoadThreadedGet(_targetScenePath);
            
            // Apply it to the correct container
            ApplyLoadedScene(loadedScene);

            // Destroy the loading screen
            FinishLoading();
        }
        else if (status == ResourceLoader.ThreadLoadStatus.Failed || status == ResourceLoader.ThreadLoadStatus.InvalidResource)
        {
            GD.PrintErr($"SceneManager: Threaded load failed for {_targetScenePath}");
            FinishLoading();
        }
        
        // Note: You can use (float)progress[0] here later if you want to update a loading progress bar!
    }

    // (Internal method) Routes the newly loaded scene to the right container
    private void ApplyLoadedScene(PackedScene newScene)
    {
        Node instance = newScene.Instantiate();

        if (instance is Node2D)
        {
            ClearContainer(WorldContainer, ref _currentWorldScene);
            WorldContainer.AddChild(instance);
            _currentWorldScene = instance;
            
            // When loading a new game world, clear the old UI out
            ClearContainer(GuiContainer, ref _currentGuiScene); 
        }
        else if (instance is Control)
        {
            // PROACTIVE FIX: If we are loading a full-screen UI (like returning to the Main Menu),
            // we must completely destroy the game world so the level doesn't stay running in the background!
            ClearContainer(WorldContainer, ref _currentWorldScene);

            ClearContainer(GuiContainer, ref _currentGuiScene);
            GuiContainer.AddChild(instance);
            _currentGuiScene = instance;
        }
    }

    // (Internal method) Cleans up the loading screen and stops processing
    private void FinishLoading()
    {
        _isLoading = false;
        SetProcess(false);

        if (_activeLoadingScreen != null && IsInstanceValid(_activeLoadingScreen))
        {
            _activeLoadingScreen.QueueFree();
            _activeLoadingScreen = null;
        }
    }

    private void ClearContainer(Node container, ref Node currentScene)
    {
        if (currentScene != null && IsInstanceValid(currentScene))
        {
            currentScene.QueueFree();
        }
        foreach (Node child in container.GetChildren())
        {
            child.QueueFree();
        }
        currentScene = null;
    }


    // NEW: Overload to handle a string path (for your Main Menu)
    public Node OpenMenuOverlay(string scenePath)
    {
        PackedScene overlayScene = GD.Load<PackedScene>(scenePath);
        return OpenMenuOverlay(overlayScene);
    }

    // UPDATED: Handles the PackedScene directly and returns the spawned instance
    public Node OpenMenuOverlay(PackedScene overlayScene)
    {
        // Prevent opening multiple overlays at once
        if (_activeOverlay != null) return null; 

        // Hide the current active UI (e.g., the HUD)
        if (_currentGuiScene is Control currentUi)
        {
            currentUi.Hide();
        }

        if (overlayScene != null)
        {
            _activeOverlay = overlayScene.Instantiate();
            
            // CRITICAL: Set the overlay to process even when the SceneTree is paused!
            _activeOverlay.ProcessMode = Node.ProcessModeEnum.Always;

            GuiContainer.AddChild(_activeOverlay);
            
            return _activeOverlay; // Return it so the caller can listen to it
        }
        
        return null;
    }

    public void CloseMenuOverlay()
    {
        if (_activeOverlay != null)
        {
            _activeOverlay.QueueFree();
            _activeOverlay = null;
        }

        // Unhide the original menu we left behind
        if (_currentGuiScene is Control currentUi)
        {
            currentUi.Show();
        }
    }
}