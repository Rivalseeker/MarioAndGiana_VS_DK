using Godot;
using Godot.Collections;

public partial class StateMachine : Node
{
    [Export] private NodePath _initialStatePath;
    
    // NEW: We need a reference to the body we are controlling
    [Export] private BasePlayer _player; 

    private State _currentState;
    private Dictionary<StringName, State> _states = new Dictionary<StringName, State>();

    // NEW: Public getter so other scripts can safely read the active state without modifying it
    public State CurrentState => _currentState;

    public override void _Ready()
    {
        // Auto-fetch the parent if we forgot to assign it in the Inspector
        if (_player == null)
        {
            _player = GetParent<BasePlayer>();
        }

        // Loop through all child nodes to find our States
        foreach (Node child in GetChildren())
        {
            if (child is State state)
            {
                // Store the state in a dictionary using its node name
                _states[state.Name] = state;
                
                // NEW: Give the state a reference to the player body!
                state.Player = _player;
                
                // Connect the transition signal
                state.Transitioned += OnChildTransitioned;
            }
        }

        // Initialize the starting state
        if (_initialStatePath != null)
        {
            _currentState = GetNode<State>(_initialStatePath);
            _currentState.Enter();
        }
        else if (GetChildCount() > 0 && GetChild(0) is State firstState)
        {
            // Fallback: If no initial state is set, just grab the first child
            _currentState = firstState;
            _currentState.Enter();
        }
    }

    public override void _Process(double delta)
    {
        _currentState?.Update(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        _currentState?.PhysicsUpdate(delta);
    }

    private void OnChildTransitioned(State state, StringName newStateName)
    {
        // Ignore transitions from states that aren't currently active
        if (state != _currentState) return;
        
        // SAFETY NET: If the state doesn't exist, print a massive error to the console!
        if (!_states.TryGetValue(newStateName, out State newState)) 
        {
            GD.PrintErr($"[StateMachine Error] '{_currentState.Name}' tried to transition to '{newStateName}', but no Node with that exact name exists in the StateMachine!");
            return;
        }

        _currentState.Exit();
        newState.Enter();
        _currentState = newState;
    }

    // NEW: Allows external scripts (like BasePlayer) to hijack the active state!
    public void ForceTransition(StringName newStateName)
    {
        if (!_states.TryGetValue(newStateName, out State newState)) 
        {
            GD.PrintErr($"[StateMachine Error] Tried to force transition to '{newStateName}', but no Node with that exact name exists!");
            return;
        }

        _currentState?.Exit();
        newState.Enter();
        _currentState = newState;
    }
}