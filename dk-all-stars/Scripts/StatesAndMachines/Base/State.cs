using Godot;

// This is the base class that Idle, GroundMove, Jump, etc. will inherit from
public partial class State : Node
{
    [Signal]
    public delegate void TransitionedEventHandler(State state, StringName newStateName);

    // The StateMachine will populate this automatically on _Ready!
    public BasePlayer Player; 

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update(double delta) { }
    public virtual void PhysicsUpdate(double delta) { }
}