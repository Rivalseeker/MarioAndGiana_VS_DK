using Godot;
using Godot.Collections;

public partial class BaseDkLevel : Node2D
{
    [Signal] public delegate void hideHintsEventHandler();


    [ExportCategory("Level Settings")]
    [Export] private PackedScene _playerScene;
    [Export] private bool _playerFacesLeft;

    // NEW: Dropdown in the Editor to pick which way Mario poses! (-1 = Left, 1 = Right)
    [Export(PropertyHint.Enum, "Left:-1,Right:1")] 
    private int _victoryFacingDirection = -1;
    
    // CHANGED: We now export an AudioStream so you can drag-and-drop your .ogg files!
    [ExportCategory("Audio")]
    [Export] public AudioStream _levelMusicTrack; 
    [Export] private AudioStream _failureMusicTrack; // NEW: Drag your failure .ogg here
    [Export] public AudioStream _stageClearMusicTrack;

    [ExportCategory("Hammer Settings")]
    [Export] private AudioStream _hammerMusicTrack;
    [Export] private PackedScene _smashEffectScene;

    [ExportCategory("Node References")]
    [Export] public Marker2D _playerSpawn;
    [Export] private Node _gizmosContainer;
    // CHANGED: Renamed to target your new Characters node
    [Export] public Node _playersContainer;
    [Export] public Node _enemiesContainer;
    [Export] public Node _environmentContainer; 
    [Export] public Node _checkpointContainer;
    [Export] public Node _captiveContainer;
    [Export] private AnimationPlayer heartAnim;
    [Export] private Node _pickupsContainer; // NEW
    [Export] public AnimationPlayer _promptAnim;
    [Export] public CanvasLayer _pauseScreen; // NEW: Drag your PauseScreen CanvasLayer here
    [Export] public Area2D _pitArea; // NEW: Drag your PitArea node here!
    [Export] public Area2D _goalArea;

    // NEW: Transition Screen References
    [ExportCategory("Transitions")]
    [Export] public ColorRect _blackScreen;
    [Export] private float _loadingScreenDelay = 2.0f; // Time in seconds to hold the black screen
    [Export(PropertyHint.File, "*.tscn")] private string _nextScene;

    [ExportCategory("Level Timer")]
    [Export] public bool UseTimer = true;
    [Export] public float StartTime = 300f; // Total seconds to complete the level
    [Export] private AudioStream _hurryUpMusicTrack;

    [ExportCategory("Debug UI")]
    [Export] public CanvasLayer _debugLayer;
    [Export] private Label _playerStateLabel;
    [Export] private Label _playerXVelocityLabel;
    [Export] private Label _playerYVelocityLabel;
    [Export] private Label _playerJumpsLabel;
    
    [ExportCategory("HUD References")]
    [Export] public Control _timerControl; // Drag your TimerControl node here
    [Export] public Label _timerLabel;     // Drag your Timer Label node here

    // Timer State Variables
    public float _currentTimer;
    public bool _isTimerActive = false;
    public bool _isHurryUp = false;
    public bool _isGameplayActive = false;
    // NEW: Tracks if the player is allowed to skip the death sequence
    private bool _canSkipDeath = false;

    public BasePlayer _activePlayer;
    public Vector2 _currentSpawnPosition;
    public Array<TileMapLayer> _tileLayers = new Array<TileMapLayer>();
    public bool _playerInGoalZone = false;

    // Add state property
    public bool IsHammerActive { get; private set; } = false;

    public override void _Ready()
    {
        // Ensure the black screen is hidden when the level first starts
        if (_blackScreen != null) _blackScreen.Hide();

        // Ensure the debug screen is hidden on start
        if (_debugLayer != null) _debugLayer.Hide();

        // 0. Initialize Timer & HUD
        if (_timerControl != null) _timerControl.Visible = UseTimer;
        if (_timerLabel != null) _timerLabel.RemoveThemeColorOverride("font_color"); // Reset to white on restart
        _currentTimer = StartTime;
        UpdateTimerUI();

        // 1. Memorize this level so the Pause Screen knows what to reload
        SessionState.CurrentLevelPath = this.SceneFilePath;

        if (_environmentContainer != null)
        {
            foreach (Node child in _environmentContainer.GetChildren())
            {
                if (child is TileMapLayer layer)
                {
                    _tileLayers.Add(layer);
                }
            }
        }

        // 2. Initialize Checkpoints and find the correct spawn point
        _currentSpawnPosition = _playerSpawn != null ? _playerSpawn.Position : Vector2.Zero;

        if (_checkpointContainer != null)
        {
            foreach (Node child in _checkpointContainer.GetChildren())
            {
                if (child is CheckpointFlag flag)
                {
                    // If this flag matches our saved session memory, make it Active and spawn here!
                    if (!string.IsNullOrEmpty(SessionState.ActiveCheckpointId) && flag.CheckpointId == SessionState.ActiveCheckpointId)
                    {
                        flag.InitializeState(true);
                        _currentSpawnPosition = flag.Position;
                    }
                    else
                    {
                        flag.InitializeState(false);
                    }
                }
            }
        }

        // 1. Play Level Music using your MusicManager Singleton!
        if (_levelMusicTrack != null && MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayTrack(_levelMusicTrack);
        }

        // 2. Spawn the player instantly so they are visible on screen!
        SpawnPlayer();

        // 3. Freeze the level and wait for the PromptAnim to finish
        if (_promptAnim != null && _promptAnim.HasAnimation("Start"))
        {
            SetGameplayActive(false); // Freeze!
            
            // THE FIX: Disable the pause screen while the intro animation plays
            if (_pauseScreen != null) 
            {
                _pauseScreen.ProcessMode = Node.ProcessModeEnum.Disabled;
            }

            _promptAnim.AnimationFinished += OnPromptAnimationFinished;
        }
        else
        {
            GD.PrintErr("PromptAnim not assigned or missing 'Start' animation! Unfreezing immediately.");
            SetGameplayActive(true);
        }

        if (_pitArea != null)
        {
            _pitArea.BodyEntered += OnPitAreaBodyEntered;
            _pitArea.AreaEntered += OnPitAreaAreaEntered;
        }

        if (_goalArea != null)
        {
            _goalArea.BodyEntered += OnGoalAreaBodyEntered;
            _goalArea.BodyExited += OnGoalAreaBodyExited;
        }
    }


    public override void _Input(InputEvent @event)
    {
        // Toggle the visibility of the Debug CanvasLayer when the key is pressed
        if (@event.IsActionPressed("PlayerDebug"))
        {
            if (_debugLayer != null)
            {
                _debugLayer.Visible = !_debugLayer.Visible;
            }
        }

        // NEW: Check for death skip input
        if (_canSkipDeath && @event.IsActionPressed("Pause"))
        {
            _canSkipDeath = false; // Prevent multiple triggers

            MusicManager.Instance.StopTrack();
            
            SessionState.RevertCollectedItems();
            TransitionToScene(this.SceneFilePath);
        }
    }

    


    // ==========================================
    // HELPER METHODS
    // ==========================================
    private CaptiveCharacter GetActiveCaptive()
    {
        if (_captiveContainer == null) return null;

        // Loop through the container and grab the first node that uses the CaptiveCharacter script!
        foreach (Node child in _captiveContainer.GetChildren())
        {
            if (child is CaptiveCharacter captive)
            {
                return captive;
            }
        }
        
        return null;
    }

    // ==========================================
    // GAMEPLAY FREEZE / UNFREEZE
    // ==========================================
    public void SetGameplayActive(bool isActive)
    {
        // NEW: Track the internal state for the timer to obey!
        _isGameplayActive = isActive; 

        Node.ProcessModeEnum mode = isActive ? Node.ProcessModeEnum.Inherit : Node.ProcessModeEnum.Disabled;

        if (_gizmosContainer != null) _gizmosContainer.ProcessMode = mode;
        if (_playersContainer != null) _playersContainer.ProcessMode = mode;
        if (_enemiesContainer != null) _enemiesContainer.ProcessMode = mode;
        if (_environmentContainer != null) _environmentContainer.ProcessMode = mode;
        if (_captiveContainer != null) _captiveContainer.ProcessMode = mode;
        if (_pickupsContainer != null) _pickupsContainer.ProcessMode = mode;
    }

    public void OnPitAreaBodyEntered(Node2D body)
    {
        // If the thing that fell into the pit is our player, trigger the flag!
        if (body is BasePlayer player)
        {
            // GD.Print("IN PIT");
            player.IsInPit = true;
        }
        // NEW: If ANY enemy (like a Barrel) falls into the pit, destroy it instantly!
        else if (body is BaseEnemy enemy)
        {
            enemy.QueueFree();
        }
    }

    public void OnPitAreaAreaEntered(Node2D area)
    {
        if (area is Egg)
        {
            area.QueueFree();
        }
    }

    public void OnPromptAnimationFinished(StringName animName)
    {
        if (animName == "Start")
        {
            SetGameplayActive(true);
            _isTimerActive = true; // NEW: Start ticking down!
            if (_pauseScreen != null) _pauseScreen.ProcessMode = Node.ProcessModeEnum.Always;
        }
        else if (animName == "TooBad")
        {
            SessionState.RevertCollectedItems(); 
            TransitionToScene(this.SceneFilePath);
        }
        else if (animName == "StageClear")
        {
            // ==========================================
            // TEMPORARY DEMO LOGIC: Clear all saved data 
            // before loading the next level.
            // ==========================================
            SessionState.ActiveCheckpointId = "";
            SessionState.ClearAllCollectedItems();

            TransitionToScene(_nextScene);
        }
    }

    public void SpawnPlayer()
    {
        // CHANGED: Update the null check
        if (_playerScene == null || _playerSpawn == null || _playersContainer == null) return;

        _activePlayer = _playerScene.Instantiate<BasePlayer>();
        
        _activePlayer.PlayerDied += OnPlayerDied;
        _activePlayer.PlayFailureMusic += OnPlayFailureMusic;
        _activePlayer.TriggerTooBadPrompt += OnTriggerTooBadPrompt;
        _activePlayer.DeathAnimationFinished += OnDeathAnimationFinished;
        _activePlayer.AllowDeathSkip += AllowDeathSkip;

        _activePlayer.Position = _currentSpawnPosition;

        // CHANGED: Add Mario to the new Players container!
        _playersContainer.AddChild(_activePlayer);
        
        if (_playerFacesLeft)
        {
            _activePlayer.UpdateFacingDirection(-1);
        }
        else
        {
            _activePlayer.UpdateFacingDirection(1);
        }
    }

    // ==========================================
    // LEVEL CLEAR LOGIC
    // ==========================================
    public void OnGoalAreaBodyEntered(Node2D body)
    {
        if (body is BasePlayer) _playerInGoalZone = true;
    }

    public void OnGoalAreaBodyExited(Node2D body)
    {
        if (body is BasePlayer) _playerInGoalZone = false;
    }

    // Made PUBLIC so future mechanics (like breaking all rivets in 100m) 
    // can trigger the exact same victory sequence!
    public void TriggerLevelClear()
    {
        // GD.Print("STAGE CLEARED!");

        // THE FIX: Mario survived! Lock his pocket items into the permanent bank.
        SessionState.CommitCollectedItems();

        // 1. Lock the player into a victory pose
        if (_activePlayer != null)
        {
            _activePlayer.IsDead = true; // Locks BasePlayer physics and PlayerIdle inputs
            _activePlayer.Velocity = Vector2.Zero;
            _activePlayer.UpdateFacingDirection(_victoryFacingDirection);
            _activePlayer.PlayAnimation("Idle"); 
        }

        // ==========================================
        // 2. FREEZE COMMONS & TRIGGER LEVEL EVENTS
        // ==========================================
        // if (_gizmosContainer != null)
        // {
        //     Node2D gizmosNode = _gizmosContainer.GetNodeOrNull<Node2D>("Gizmos");
        //     if (gizmosNode != null)
        //     {
        //         gizmosNode.ProcessMode = Node.ProcessModeEnum.Disabled;
        //         gizmosNode.Hide(); 
        //     }
        // }

        if (_gizmosContainer != null)
        {
            _gizmosContainer.ProcessMode = Node.ProcessModeEnum.Disabled;
        }

        if (_enemiesContainer != null)
        {
            Node2D commonsNode = _enemiesContainer.GetNodeOrNull<Node2D>("Commons");
            if (commonsNode != null)
            {
                commonsNode.ProcessMode = Node.ProcessModeEnum.Disabled;
                commonsNode.Hide(); 
            }

            // Find Donkey Kong
            DonkeyKong dk = _enemiesContainer.GetNodeOrNull<Node2D>("Bosses")?.GetNodeOrNull<DonkeyKong>("DonkeyKong");
            if (dk != null)
            {
                EnemyStateMachine dkStateMachine = dk.GetNodeOrNull<EnemyStateMachine>("StateMachine");
                if (dkStateMachine != null)
                {
                    dkStateMachine.ForceTransition("DkGrabAndRun");
                }

                // Connect DK's escape signal safely
                if (!dk.IsConnected(DonkeyKong.SignalName.DkStartedEscape, new Callable(this, MethodName.OnDkStartedEscape)))
                {
                    dk.Connect(DonkeyKong.SignalName.DkStartedEscape, new Callable(this, MethodName.OnDkStartedEscape));
                    // GD.Print("CONNTECTED");
                }
            }
        }

        // Find the Captive and instantly play her "Saved" animation!
        CaptiveCharacter captive = GetActiveCaptive();
        if (captive != null)
        {
            captive.PlaySavedAnimation();
            // GD.Print("NOTNULL");
        }

        // 3. Stop Level Music & Play Stage Clear Music
        if (MusicManager.Instance != null && _stageClearMusicTrack != null)
        {
            MusicManager.Instance.PlayTrack(_stageClearMusicTrack);
        }
        else if (MusicManager.Instance != null)
        {
            MusicManager.Instance.StopTrack(); // Fallback if no victory track is assigned
        }

        // 4. Disable Pause Screen
        if (_pauseScreen != null) 
        {
            _pauseScreen.ProcessMode = Node.ProcessModeEnum.Disabled;
        }

        // 5. Play the Prompt Animation
        if (_promptAnim != null && _promptAnim.HasAnimation("StageClear"))
        {
            _promptAnim.Play("StageClear");
        }

        if (heartAnim != null && heartAnim.HasAnimation("Show"))
        {
            heartAnim.Play("Show");
        }
    }

    private void OnDkStartedEscape()
    {
        CaptiveCharacter captive = GetActiveCaptive();
        DonkeyKong dk = _enemiesContainer?.GetNodeOrNull<Node2D>("Bosses")?.GetNodeOrNull<DonkeyKong>("DonkeyKong");

        if (captive != null && dk != null)
        {
            // 1. Play the kidnapped animation
            captive.PlayTakenAwayAnimation();
            
            // 2. Reparent the captive to DK so she perfectly follows his climb!
            captive.GetParent().RemoveChild(captive);
            dk.AddChild(captive);
            
            // 3. Apply the offset so she sits exactly where you want her.
            captive.Position = new Vector2(dk.CaptiveCarryOffset.X * dk.EscapeDirection, dk.CaptiveCarryOffset.Y);
        }
    }

    // --- DEATH SEQUENCE RESPONSES ---
    private void OnPlayerDied()
    {
        // 1. Instantly stop the current background music
        if (MusicManager.Instance != null) MusicManager.Instance.StopTrack();

        // 2. Disable the Pause Screen so the player can't interrupt the sequence
        if (_pauseScreen != null) _pauseScreen.SetDeferred(Node.PropertyName.ProcessMode, (int)Node.ProcessModeEnum.Disabled);

        // 3. FREEZE ALL ENTITIES (Safely deferred to avoid physics crashes)
        CallDeferred(MethodName.FreezeEntitiesDeferred);

        EmitSignal(SignalName.hideHints);
    }

    private void FreezeEntitiesDeferred()
    {
        if (_gizmosContainer != null)
        {
            _gizmosContainer.ProcessMode = Node.ProcessModeEnum.Disabled;
        }

        if (_enemiesContainer != null)
        {
            _enemiesContainer.ProcessMode = Node.ProcessModeEnum.Disabled;
        }

        // Freeze moving platforms or environment hazards as well
        if (_environmentContainer != null)
        {
            _environmentContainer.ProcessMode = Node.ProcessModeEnum.Disabled;
        }

        // THE FIX: Freeze the captive so she stops animating when Mario dies!
        if (_captiveContainer != null)
        {
            _captiveContainer.ProcessMode = Node.ProcessModeEnum.Disabled;
        }

        if (_pickupsContainer != null) _pickupsContainer.ProcessMode = Node.ProcessModeEnum.Disabled;

        // Ensure Players container renders cleanly on top of the frozen enemies
        if (_playersContainer is Node2D playersNode)
        {
            playersNode.ZIndex = 4;
        }
    }

    private void AllowDeathSkip()
    {
        _canSkipDeath = true;
    }

    private void OnPlayFailureMusic()
    {
        if (MusicManager.Instance != null && _failureMusicTrack != null)
        {
            MusicManager.Instance.PlayTrack(_failureMusicTrack);
        }
    }

    private void OnTriggerTooBadPrompt()
    {
        if (_promptAnim != null && _promptAnim.HasAnimation("TooBad"))
        {
            _promptAnim.Play("TooBad");
        }
    }

    private void OnDeathAnimationFinished()
    {
        // DO NOTHING! 
        // We previously handled the reload here, but now we wait for the 
        // "TooBad" prompt animation to finish before transitioning.
    }

    // ==========================================
// HAMMER LOGIC
// ==========================================
    public void StartHammerMusic()
    {
        IsHammerActive = true;
        UpdateFireballPalettes(true);

        if (MusicManager.Instance != null && _hammerMusicTrack != null)
            MusicManager.Instance.PlayTrack(_hammerMusicTrack);
    }

    public void StopHammerMusic()
    {
        IsHammerActive = false;
        UpdateFireballPalettes(false);

        if (MusicManager.Instance != null)
        {
            if (_isHurryUp && _hurryUpMusicTrack != null)
            {
                MusicManager.Instance.PlayTrack(_hurryUpMusicTrack);
            }
            else if (_levelMusicTrack != null)
            {
                MusicManager.Instance.PlayTrack(_levelMusicTrack);
            }
        }
    }

    private void UpdateFireballPalettes(bool active)
    {
        UpdateFireballsRecursive(this, active);
    }

    private void UpdateFireballsRecursive(Node node, bool active)
    {
        if (node is Fireball fireball)
        {
            fireball.SetHammerPalette(active);
        }

        foreach (Node child in node.GetChildren())
        {
            UpdateFireballsRecursive(child, active);
        }
    }

    // Called directly by the Hammer script when it hits a valid target
    public async void TriggerHammerSmash(Node2D entityToDestroy, Vector2 hitPosition)
    {
        // 1. Freeze all gameplay elements
        SetGameplayActive(false);

        // 2. Destroy the enemy instantly
        if (IsInstanceValid(entityToDestroy))
        {
            entityToDestroy.QueueFree();
        }

        // 3. Spawn the Smash Visual Effect
        if (_smashEffectScene != null)
        {
            Node2D effect = _smashEffectScene.Instantiate<Node2D>();
            
            // CRITICAL FIX: Force the effect to process even while the rest of the game is frozen!
            effect.ProcessMode = Node.ProcessModeEnum.Always;
            
            AddChild(effect);
            effect.GlobalPosition = hitPosition;

            // Find the AnimationPlayer (Assumes it's named "AnimationPlayer" and is a direct child of the root)
            AnimationPlayer animPlayer = effect.GetNodeOrNull<AnimationPlayer>("ImpactAnim");

            if (animPlayer != null && animPlayer.HasAnimation("EntityDefeated"))
            {
                animPlayer.Play("EntityDefeated");
                
                // THE FIX: Wait dynamically for the animation to conclude!
                await ToSignal(animPlayer, AnimationPlayer.SignalName.AnimationFinished);
            }
            else
            {
                // Fallback in case the animation node isn't found
                GD.PrintErr("Could not find AnimationPlayer or 'EntityDefeated' animation on the impact scene.");
                await ToSignal(GetTree().CreateTimer(0.5f, true, false, true), SceneTreeTimer.SignalName.Timeout);
            }

            // Cleanup the impact scene from the level once the animation is done
            effect.QueueFree();
        }
        else
        {
            // Fallback if the impact scene wasn't assigned in the inspector
            await ToSignal(GetTree().CreateTimer(0.5f, true, false, true), SceneTreeTimer.SignalName.Timeout);
        }

        // 4. Unfreeze the level (only if Mario didn't die from a projectile exactly on the same frame)
        if (_activePlayer != null && !_activePlayer.IsDead)
        {
            SetGameplayActive(true);
        }
    }

    // ==========================================
    // SCENE TRANSITION LOGIC
    // ==========================================
    public async void TransitionToScene(string targetScenePath)
    {
        // 1. Show the black screen
        if (_blackScreen != null) _blackScreen.Show();

        // 2. Hide the pause menu if it's currently open
        if (_pauseScreen != null) _pauseScreen.Hide();

        // THE FIX: 3. Pause the entire engine so absolutely nothing runs in the background
        GetTree().Paused = true;

        // 4. Wait for the designated delay time. 
        // (The 'true' parameter ensures this specific timer keeps ticking even while the tree is paused!)
        await ToSignal(GetTree().CreateTimer(_loadingScreenDelay, true, false, true), SceneTreeTimer.SignalName.Timeout);

        // THE FIX: 5. Unpause the engine right before loading so the new scene doesn't start frozen!
        GetTree().Paused = false;

        // 6. Load the scene
        if (SceneManager.Instance != null && !string.IsNullOrEmpty(targetScenePath))
        {
            SceneManager.Instance.LoadSceneAsync(targetScenePath);
        }
        else
        {
            GetTree().ReloadCurrentScene(); // Fallback
        }
    }

    // ==========================================
    // TIMER & DEBUG LOGIC
    // ==========================================
    public override void _Process(double delta)
    {
        // NEW: Update Debug UI if it is visible and the player exists
        if (_debugLayer != null && _debugLayer.Visible && _activePlayer != null)
        {
            UpdateDebugUI();
        }

        // Only tick down if the timer is enabled, the game isn't frozen, and Mario is alive/playing
        if (!UseTimer || !_isTimerActive || !_isGameplayActive || _activePlayer == null || _activePlayer.IsDead || _playerInGoalZone) 
            return;

        _currentTimer -= (float)delta;

        // Check for Hurry Up state
        if (_currentTimer <= 30.0f && !_isHurryUp)
        {
            TriggerHurryUp();
        }

        // Check for Death state
        if (_currentTimer <= 0)
        {
            _currentTimer = 0;
            UpdateTimerUI();
            TriggerTimeUp();
            return; 
        }

        UpdateTimerUI();
    }

    public void UpdateDebugUI()
    {
        // 1. Fetch the State Machine's current state directly!
        if (_playerStateLabel != null && _activePlayer.PlayerStateMachine != null)
        {
            // THE FIX: Directly access the new public CurrentState property
            State activeState = _activePlayer.PlayerStateMachine.CurrentState;
            string stateName = activeState != null ? activeState.Name.ToString() : "Unknown";
            
            _playerStateLabel.Text = $"State: {stateName}";
        }

        // 2. Fetch Velocity
        if (_playerXVelocityLabel != null) 
            _playerXVelocityLabel.Text = $"X Vel: {_activePlayer.Velocity.X:F2}";
            
        if (_playerYVelocityLabel != null) 
            _playerYVelocityLabel.Text = $"Y Vel: {_activePlayer.Velocity.Y:F2}";

        // 3. Fetch Jump Count
        if (_playerJumpsLabel != null) 
            _playerJumpsLabel.Text = $"Jumps: {_activePlayer.CurrentJumps}";
    }

    public void UpdateTimerUI()
    {
        if (_timerLabel != null)
        {
            // Format to show whole numbers (e.g., 300, 299)
            _timerLabel.Text = Mathf.CeilToInt(_currentTimer).ToString();
        }
    }

    public void TriggerHurryUp()
    {
        _isHurryUp = true;
        
        // Turn the text red!
        if (_timerLabel != null)
        {
            _timerLabel.AddThemeColorOverride("font_color", Colors.Red);
        }

        // NEW: Play the Hurry Up animation on the PromptAnim node!
        if (_promptAnim != null && _promptAnim.HasAnimation("HurryUp"))
        {
            _promptAnim.Play("HurryUp");
        }

        // Only swap the music if Mario is NOT currently swinging the hammer
        if (_activePlayer != null && !_activePlayer.HasHammer)
        {
            if (MusicManager.Instance != null && _hurryUpMusicTrack != null)
            {
                MusicManager.Instance.PlayTrack(_hurryUpMusicTrack);
            }
        }
    }

    public void TriggerTimeUp()
    {
        _isTimerActive = false;
        
        // Naturally triggers your entire death sequence, freeze frames, and UI reloading!
        if (_activePlayer != null && !_activePlayer.IsDead)
        {
            _activePlayer.Die();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_activePlayer == null) return;

        // NEW: Stop running level logic (and prevent spamming the victory method) if the sequence started
        if (_activePlayer.IsDead) return;

        CheckLadderStatus();

        // THE FIX: Only trigger victory if Mario is touching the goal AND has safely entered the Idle/GroundMove state!
        if (_playerInGoalZone && (_activePlayer.IsIdle || _activePlayer.IsGroundMove))
        {
            TriggerLevelClear();
        }
    }

    // ==========================================
    // LADDER DETECTION LOGIC
    // ==========================================
    public void CheckLadderStatus()
    {
        bool foundCenterLadder = false;
        bool foundFeetLadder = false;

        // Default to 0 each physics frame before checking layers
        _activePlayer.CurrentLadderVelocityY = 0f;
                
        Vector2 centerPos = _activePlayer.GlobalPosition;
        Vector2 feetPos = _activePlayer.LadderDetector.GlobalPosition + _activePlayer.LadderDetector.TargetPosition;

        foreach (TileMapLayer layer in _tileLayers)
        {
            Vector2I centerCell = layer.LocalToMap(layer.ToLocal(centerPos));
            TileData centerData = layer.GetCellTileData(centerCell);
            
            if (centerData != null && (bool)centerData.GetCustomData("IsLadder"))
            {
                foundCenterLadder = true;
                _activePlayer.CurrentLadderX = layer.ToGlobal(layer.MapToLocal(centerCell)).X;
                CalculateLadderBounds(layer, centerCell);

                if (layer is LevelTileMap1 scrollingLayer && scrollingLayer.IsScrolling)
                {
                    _activePlayer.CurrentLadderVelocityY = scrollingLayer.ScrollSpeed;
                }
            }

            Vector2I feetCell = layer.LocalToMap(layer.ToLocal(feetPos));
            TileData feetData = layer.GetCellTileData(feetCell);
            
            if (feetData != null && (bool)feetData.GetCustomData("IsLadder"))
            {
                foundFeetLadder = true;
                
                if (!foundCenterLadder) 
                {
                    _activePlayer.CurrentLadderX = layer.ToGlobal(layer.MapToLocal(feetCell)).X;
                    CalculateLadderBounds(layer, feetCell);

                    if (layer is LevelTileMap1 scrollingLayer && scrollingLayer.IsScrolling)
                    {
                        _activePlayer.CurrentLadderVelocityY = scrollingLayer.ScrollSpeed;
                    }
                }
            }
        }

        _activePlayer.CanClimb = foundCenterLadder;
        _activePlayer.CanClimbDownLedge = foundFeetLadder && _activePlayer.IsOnFloor();
    }

    private void CalculateLadderBounds(TileMapLayer layer, Vector2I startCell)
    {
        Vector2I currentCell = startCell;
        
        while (IsLadderCell(layer, currentCell)) { currentCell.Y -= 1; }
        
        Vector2I topLadderCell = new Vector2I(currentCell.X, currentCell.Y + 1);
        TileData topData = layer.GetCellTileData(topLadderCell);
        _activePlayer.CurrentLadderHasLedge = topData != null && (bool)topData.GetCustomData("IsLedge");

        _activePlayer.CurrentLadderTopY = layer.ToGlobal(layer.MapToLocal(currentCell)).Y + (layer.TileSet.TileSize.Y / 2.0f);

        currentCell = startCell;
        while (IsLadderCell(layer, currentCell)) { currentCell.Y += 1; }
        _activePlayer.CurrentLadderBottomY = layer.ToGlobal(layer.MapToLocal(currentCell)).Y - (layer.TileSet.TileSize.Y / 2.0f);
    }

    // private void CalculateLadderBounds(TileMapLayer layer, Vector2I startCell)
    // {
    //     // ==========================================
    //     // 1. FIND THE TOP LADDER BOUNDARY
    //     // ==========================================
    //     Vector2I currentCell = startCell;
        
    //     // Move up until we find the empty space exactly above the ladder chain
    //     while (IsLadderCell(layer, currentCell)) { currentCell.Y -= 1; }
        
    //     // Drop down one cell to target the absolute topmost ladder tile
    //     Vector2I topLadderCell = new Vector2I(currentCell.X, currentCell.Y + 1);
        
    //     // Find the precise global center of that ladder tile
    //     Vector2 topLadderGlobalCenter = layer.ToGlobal(layer.MapToLocal(topLadderCell));

    //     bool foundLedge = false;
    //     float actualLedgeTopY = 0f;

    //     // THE FIX: Loop through ALL layers to see if a ledge shares this exact global space!
    //     foreach (TileMapLayer l in _tileLayers)
    //     {
    //         // Convert the global position into this specific layer's coordinate system
    //         Vector2I localCell = l.LocalToMap(l.ToLocal(topLadderGlobalCenter));
    //         TileData data = l.GetCellTileData(localCell);
            
    //         if (data != null && (bool)data.GetCustomData("IsLedge"))
    //         {
    //             foundLedge = true;
                
    //             // Calculate the exact top pixel edge of the ledge on THIS specific layer.
    //             // This perfectly absorbs your (0, -1) to (0, -7) positional offsets!
    //             Vector2I emptyCellAboveLedge = new Vector2I(localCell.X, localCell.Y - 1);
    //             actualLedgeTopY = l.ToGlobal(l.MapToLocal(emptyCellAboveLedge)).Y + (l.TileSet.TileSize.Y / 2.0f);
                
    //             break; // We found the ledge, stop searching other layers
    //         }
    //     }

    //     _activePlayer.CurrentLadderHasLedge = foundLedge;

    //     if (foundLedge)
    //     {
    //         // If we found a ledge on any layer, use its perfectly calculated offset height!
    //         _activePlayer.CurrentLadderTopY = actualLedgeTopY;
    //     }
    //     else
    //     {
    //         // If it's a truly broken ladder with no ledge, default to the standard Layer 1 height
    //         _activePlayer.CurrentLadderTopY = layer.ToGlobal(layer.MapToLocal(currentCell)).Y + (layer.TileSet.TileSize.Y / 2.0f);
    //     }

    //     // ==========================================
    //     // 2. FIND THE BOTTOM LADDER BOUNDARY
    //     // ==========================================
    //     currentCell = startCell;
        
    //     // Move down until we find the empty space exactly below the ladder chain
    //     while (IsLadderCell(layer, currentCell)) { currentCell.Y += 1; }
        
    //     // Calculate the bottom edge of the bottommost ladder tile
    //     _activePlayer.CurrentLadderBottomY = layer.ToGlobal(layer.MapToLocal(currentCell)).Y - (layer.TileSet.TileSize.Y / 2.0f);
    // }

    private bool IsLadderCell(TileMapLayer layer, Vector2I cell)
    {
        TileData data = layer.GetCellTileData(cell);
        return data != null && (bool)data.GetCustomData("IsLadder");
    }
}