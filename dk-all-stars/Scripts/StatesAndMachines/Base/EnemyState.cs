using Godot;

public partial class EnemyState : Node
{
    [Signal]
    public delegate void TransitionedEventHandler(EnemyState state, StringName newStateName);

    // The EnemyStateMachine will populate this automatically on _Ready!
    public BaseEnemy Enemy; 

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update(double delta) { }
    public virtual void PhysicsUpdate(double delta) { }
}