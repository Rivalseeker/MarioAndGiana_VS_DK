using Godot;
using Godot.Collections;

public partial class Mario20mL1 : BaseDkLevel
{
    [ExportCategory("Fireball Spawner Settings")]
    [Export] private PackedScene _fireballScene;
    [Export] private float _fireballSpawnInterval = 4.0f; // Developer-specified interval
    [Export] private int _maxFireballs = 6;
    
    // Arrays to hold your Marker2D nodes
    [Export] private Array<Marker2D> _leftEnemySpawns = new Array<Marker2D>();  // Spawns 1-4
    [Export] private Array<Marker2D> _rightEnemySpawns = new Array<Marker2D>(); // Spawns 5-8

    private float _fireballSpawnTimer = 0f;

    [ExportCategory("Cutscene Animation")]
    [Export] public AnimationPlayer MarioCompleteAnim;

    // NEW: Variables to track the rivets
    [ExportCategory("Rivet Tracker")]
    [Export] private Node _rivetsContainer; // Drag your 'Rivets' folder node here!
    private int _rivetsRemoved = 0;
    private int _totalRivets = 8;

    // NEW: Cutscene Control Exports
    [ExportCategory("Cutscene Method References")]
    [Export] private AnimationPlayer _dkAnimPlayer; // Drag Donkey Kong's AnimationPlayer here
    [Export] private AudioStream _rivetVictoryMusicTrack; // Assign your new victory song here
    [Export] private CharacterBody2D _dkCharacter;
    [Export] private RemoteTransform2D _marioCutsceneTransform;

    // NEW: Add a reference to where your belts are stored
    [Export] private Node _conveyorsContainer;




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
        }

        if (_goalArea != null)
        {
            _goalArea.BodyEntered += OnGoalAreaBodyEntered;
            _goalArea.BodyExited += OnGoalAreaBodyExited;
        }

        _fireballSpawnTimer = _fireballSpawnInterval;

        // NEW: Loop through the Rivets container and connect their signals
        if (_rivetsContainer != null)
        {
            foreach (Node child in _rivetsContainer.GetChildren())
            {
                if (child is Rivet rivet)
                {
                    rivet.RivetRemoved += OnRivetRemoved;
                }
            }
        }
    }


    public override void _Process(double delta)
    {
        // NEW: Debug input to instantly complete the level
        // Pressing F12 will instantly delete rivets and trigger the cutscene
        if (Input.IsPhysicalKeyPressed(Key.F12)) 
        {
            TriggerDebugVictory();
        }

        // NEW: Update Debug UI if it is visible and the player exists
        if (_debugLayer != null && _debugLayer.Visible && _activePlayer != null)
        {
            UpdateDebugUI();
        }

        // NEW: Handle Fireball Spawning if the game is active and the player is alive
        if (_isGameplayActive && _activePlayer != null && !_activePlayer.IsDead && _fireballScene != null)
        {
            HandleFireballSpawning(delta);
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

    // ==========================================
    // FIREBALL SPAWNER LOGIC
    // ==========================================
    private int GetCurrentFireballCount()
    {
        int count = 0;
        
        // Target either the "Commons" sub-node or the main enemies container
        Node targetContainer = _enemiesContainer?.GetNodeOrNull<Node2D>("Commons") ?? _enemiesContainer;
        
        if (targetContainer != null)
        {
            foreach (Node child in targetContainer.GetChildren())
            {
                // Make sure it's a Fireball and hasn't just been flagged for destruction
                if (child is Fireball && !child.IsQueuedForDeletion())
                {
                    count++;
                }
            }
        }
        
        return count;
    }

    private void HandleFireballSpawning(double delta)
    {
        _fireballSpawnTimer -= (float)delta;

        if (_fireballSpawnTimer <= 0)
        {
            // Reset the timer regardless of whether we spawn or not
            // This ensures they space out properly once one is destroyed
            _fireballSpawnTimer = _fireballSpawnInterval;

            // NEW: Only spawn if we are under the maximum limit
            if (GetCurrentFireballCount() < _maxFireballs)
            {
                SpawnFireball();
            }
        }
    }

    private void SpawnFireball()
    {
        if (_leftEnemySpawns.Count == 0 && _rightEnemySpawns.Count == 0) return;

        float playerX = _activePlayer.GlobalPosition.X;
        Array<Marker2D> validSpawns = new Array<Marker2D>();

        // Logic: Mario X < 192 (Use Right Spawns), X > 192 (Use Left Spawns), X == 192 (Use Any)
        if (playerX < 192)
        {
            validSpawns = _rightEnemySpawns;
        }
        else if (playerX > 192)
        {
            validSpawns = _leftEnemySpawns;
        }
        else 
        {
            // Exactly 192: Combine both arrays
            foreach (var spawn in _leftEnemySpawns) validSpawns.Add(spawn);
            foreach (var spawn in _rightEnemySpawns) validSpawns.Add(spawn);
        }

        if (validSpawns.Count == 0) return;

        // Pick a random Marker2D from the valid list
        int randomIndex = GD.RandRange(0, validSpawns.Count - 1);
        Marker2D chosenSpawnPoint = validSpawns[randomIndex];

        // Instantiate the Fireball
        Fireball newFireball = _fireballScene.Instantiate<Fireball>();
        newFireball.GlobalPosition = chosenSpawnPoint.GlobalPosition;

        // Add to the Enemies container (or Commons if you have a sub-node for them)
        Node2D commonsNode = _enemiesContainer?.GetNodeOrNull<Node2D>("Commons");
        if (commonsNode != null)
        {
            commonsNode.AddChild(newFireball);
        }
        else if (_enemiesContainer != null)
        {
            _enemiesContainer.AddChild(newFireball);
        }

        // Toggle the specific Sprites exactly as requested
        // Using the exact node names referenced in your Fireball.cs script[cite: 3]
        Sprite2D sprite1 = newFireball.GetNodeOrNull<Sprite2D>("FireballSprite1");
        Sprite2D sprite2 = newFireball.GetNodeOrNull<Sprite2D>("FireballSprite2");

        if (sprite1 != null) sprite1.Hide();
        if (sprite2 != null) sprite2.Show();
    }

    // NEW: Call this method when all Rivets are cleared!
    public void TriggerRivetVictorySequence()
    {
        // 1. Commit Items and stop the level timers
        SessionState.CommitCollectedItems();
        _isTimerActive = false;
        _isGameplayActive = false;

        // 2. Lock Mario's internal physics and state machine inputs
        if (_activePlayer != null)
        {
            _activePlayer.PlayerStateMachine.ForceTransition("MainComplete");
        }

        // 3. FREEZE ENTITIES (EXCLUDING BOSSES & PLAYERS & CAPTIVES)
        // if (_playersContainer != null) _playersContainer.ProcessMode = Node.ProcessModeEnum.Disabled;
        
        // CHANGED: Only disable the "Commons" container (Fireballs/Barrels) so DK is NOT paused
        Node2D commonsContainer = _enemiesContainer?.GetNodeOrNull<Node2D>("Commons");
        if (commonsContainer != null) 
        {
            commonsContainer.ProcessMode = Node.ProcessModeEnum.Disabled;
            commonsContainer.Hide();
        }
        
        // NEW: Force DK into his defeat state so he stops acting and gives control to the cutscene
        if (_dkCharacter is DonkeyKong dk)
        {
            if (dk.StateMachine != null)
            {
                dk.StateMachine.CallDeferred(EnemyStateMachine.MethodName.ForceTransition, "DkMarioDefeat");
            }
        }

        // 4. Handle Music and Pause Menu UI
        if (MusicManager.Instance != null && _stageClearMusicTrack != null)
        {
            MusicManager.Instance.PlayTrack(_stageClearMusicTrack);
        }
        
        if (_pauseScreen != null) 
        {
            _pauseScreen.ProcessMode = Node.ProcessModeEnum.Disabled;
        }

        // 5. Play the Master Cutscene!
        if (MarioCompleteAnim != null && MarioCompleteAnim.HasAnimation("MarioComplete"))
        {
            MarioCompleteAnim.ProcessMode = Node.ProcessModeEnum.Always; 
            MarioCompleteAnim.Play("MarioComplete");
        }
    }

    // NEW: Handles the signal broadcasted by the Rivets
    private void OnRivetRemoved(Rivet rivet)
    {
        _rivetsRemoved++;

        // Update the specific Rivet's text dynamically right before it floats up!
        if (rivet.Tally != null)
        {
            rivet.Tally.Text = $"{_rivetsRemoved}/{_totalRivets}";
        }

        // If we hit the max amount, trigger the end sequence
        if (_rivetsRemoved >= _totalRivets)
        {
            TriggerRivetVictorySequence();
        }
    }

    // ==========================================
    // CUTSCENE METHOD CALL TRACK TARGETS
    // ==========================================

    /// <summary>
    /// Call this from the AnimationPlayer track to trigger Donkey Kong's falling/defeat animation.
    /// </summary>
    public void PlayDkDefeatAnimation()
    {
        if (_dkAnimPlayer != null && _dkAnimPlayer.HasAnimation("MarioDefeatsDK"))
        {
            _dkAnimPlayer.Play("MarioDefeatsDK");
        }
        else
        {
            GD.PrintErr("DK AnimationPlayer is not assigned or is missing the 'MarioDefeatsDK' animation!");
        }
    }

    /// <summary>
    /// Call this from the AnimationPlayer track to play the rivet completion victory song.
    /// </summary>
    public void PlayRivetVictoryMusic()
    {
        if (MusicManager.Instance != null && _rivetVictoryMusicTrack != null)
        {
            MusicManager.Instance.PlayTrack(_rivetVictoryMusicTrack); //[cite: 22]
        }
        else
        {
            GD.PrintErr("MusicManager instance is missing or Rivet Victory Music Track is not assigned!");
        }
    }

    /// <summary>
    /// Call this from the AnimationPlayer track to instantly snap Mario to a global Vector2 coordinate.
    /// </summary>
    public void SnapMarioToPosition(Vector2 globalPos)
    {
        if (_activePlayer != null)
        {
            _activePlayer.GlobalPosition = globalPos;

            // if (!_)
        }
    }

    public void SnapDkToPosition(Vector2 globalPos)
    {
        if (_dkCharacter != null)
        {
            _dkCharacter.GlobalPosition = globalPos;
        }
    }

    /// <summary>
    /// Call this from the AnimationPlayer track to freeze all conveyor belts.
    /// </summary>
    public void HaltAllConveyors()
    {
        // Use the dedicated container if assigned, otherwise fallback to scanning the whole environment
        Node targetContainer = _conveyorsContainer != null ? _conveyorsContainer : _environmentContainer;
        
        if (targetContainer != null)
        {
            FindAndStopConveyors(targetContainer);
        }
    }

    // Recursive helper to ensure we find every single belt, even if they are nested in folders
    private void FindAndStopConveyors(Node parent)
    {
        foreach (Node child in parent.GetChildren())
        {
            if (child is ConveyorBelt belt)
            {
                belt.StopBelt();
            }
            
            // Keep digging if this node has its own children
            if (child.GetChildCount() > 0)
            {
                FindAndStopConveyors(child);
            }
        }
    }

    private void PlayMarioMoveCompleteAnimation()
    {
        _activePlayer.PlayerAnim.Play("MoveComplete");
    }

    /// <summary>
    /// Call this from the AnimationPlayer track to snap Mario to the RemoteTransform2D.
    /// </summary>
    public void BindMarioToCutsceneTransform()
    {
        if (_activePlayer != null && _marioCutsceneTransform != null)
        {
            // We use GetPathTo() to ensure the NodePath is perfectly formatted 
            // relative to the RemoteTransform2D node itself.
            _marioCutsceneTransform.RemotePath = _marioCutsceneTransform.GetPathTo(_activePlayer);
            
            // Optional: You can force Mario to immediately snap to the transform's current location
            _marioCutsceneTransform.ForceUpdateCache();
        }
        else
        {
            GD.PrintErr("Cannot bind Mario: Active Player or RemoteTransform2D is missing!");
        }
    }

    // /// <summary>
    // /// Call this from the AnimationPlayer track to release Mario when the animation is finished.
    // /// </summary>
    // public void UnbindMarioFromCutsceneTransform()
    // {
    //     if (_marioCutsceneTransform != null)
    //     {
    //         // Passing an empty string clears the NodePath and stops driving Mario
    //         _marioCutsceneTransform.RemotePath = new NodePath("");
    //     }
    // }

    




    // ==========================================
    // DEBUG / CHEATS (TO BE REMOVED LATER)
    // ==========================================
    private void TriggerDebugVictory()
    {
        // If the sequence has already started, do nothing so we don't fire it twice
        if (!_isGameplayActive) return;

        GD.Print("DEBUG: Instant Victory Triggered!");

        // 1. Instantly destroy all Rivets in the scene
        if (_rivetsContainer != null)
        {
            foreach (Node child in _rivetsContainer.GetChildren())
            {
                // QueueFree removes them from the scene tree at the end of the frame
                child.QueueFree(); 
            }
        }

        // 2. Force the internal counter to max just in case
        _rivetsRemoved = _totalRivets;

        // 3. Fire your standard victory cutscene logic
        TriggerRivetVictorySequence();
    }
}
