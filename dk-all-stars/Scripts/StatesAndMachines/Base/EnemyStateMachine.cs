using Godot;
using Godot.Collections;

public partial class EnemyStateMachine : Node
{
    [Export] private NodePath _initialStatePath;
    
    // Looks for a BaseEnemy instead of a BasePlayer!
    [Export] private BaseEnemy _enemy; 

    private EnemyState _currentState;
    private Dictionary<StringName, EnemyState> _states = new Dictionary<StringName, EnemyState>();
    
    // Allow external scripts to see the active state
    public EnemyState CurrentState => _currentState;

    public override void _Ready()
    {
        if (_enemy == null)
        {
            _enemy = GetParent<BaseEnemy>();
        }

        foreach (Node child in GetChildren())
        {
            if (child is EnemyState state)
            {
                _states[state.Name] = state;
                state.Enemy = _enemy; // Pass the enemy reference!
                state.Transitioned += OnChildTransitioned;
            }
        }

        if (_initialStatePath != null)
        {
            _currentState = GetNode<EnemyState>(_initialStatePath);
            _currentState.Enter();
        }
        else if (GetChildCount() > 0 && GetChild(0) is EnemyState firstState)
        {
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

    private void OnChildTransitioned(EnemyState state, StringName newStateName)
    {
        if (state != _currentState) return;
        
        if (!_states.TryGetValue(newStateName, out EnemyState newState)) 
        {
            GD.PrintErr($"[EnemyStateMachine Error] '{_currentState.Name}' tried to transition to '{newStateName}', but it doesn't exist!");
            return;
        }

        _currentState.Exit();
        newState.Enter();
        _currentState = newState;
    }

    // NEW: Allows external scripts (like the Level Manager) to hijack the active state!
    public void ForceTransition(StringName newStateName)
    {
        if (!_states.TryGetValue(newStateName, out EnemyState newState)) 
        {
            GD.PrintErr($"[EnemyStateMachine Error] Tried to force transition to '{newStateName}', but it doesn't exist!");
            return;
        }

        _currentState?.Exit();
        newState.Enter();
        _currentState = newState;
    }
}