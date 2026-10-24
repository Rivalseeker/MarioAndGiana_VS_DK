using Godot;

public partial class FireballOiled : EnemyState
{
    private Tween _scaleTween;
    private double _timer;
    private bool _isShrinking;
    
    // Memory tracker
    private StringName _returnState;

    public override void Enter()
    {
        if (Enemy is Fireball fireball)
        {
            fireball._sfxHit.Play();

            _returnState = fireball.PreOiledState;

            fireball.Velocity = Vector2.Zero;
            
            if (_returnState == "FireballClimb")
            {
                fireball.IsClimbing = true;
            }
            else
            {
                fireball.IsClimbing = false; 
            }
            
            _timer = fireball.OiledDuration;
            _isShrinking = false;

            _scaleTween?.Kill();
            _scaleTween = CreateTween();
            
            // THE FIX: Use TweenMethod and target our custom stepping function!
            // We use Linear transition so the timing between steps feels perfectly rhythmic.
            _scaleTween.TweenMethod(Callable.From<float>(ApplySteppedScale), 1.0f, 5.0f, fireball.OiledScaleTime)
                       .SetTrans(Tween.TransitionType.Linear);
        }
    }

    // NEW: Custom method to force the scale into rigid 1.0 increments
    private void ApplySteppedScale(float rawScale)
    {
        if (Enemy is Fireball fireball)
        {
            // Round the smooth tween number to the nearest whole number (1, 2, 3, or 4)
            float steppedScale = Mathf.Round(rawScale);
            fireball.Scale = new Vector2(steppedScale, steppedScale);
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        if (Enemy is not Fireball fireball) return;

        Vector2 velocity = fireball.Velocity;
        
        if (!fireball.IsClimbing && !fireball.IsOnFloor())
        {
            velocity.Y += fireball.FallGravity * (float)delta;
        }
        else
        {
            velocity.X = 0; 
            if (fireball.IsClimbing) velocity.Y = 0; 
        }
        
        fireball.Velocity = velocity;

        if (!_isShrinking)
        {
            _timer -= delta;
            
            if (_timer <= 0)
            {
                _isShrinking = true;
                
                _scaleTween?.Kill();
                _scaleTween = CreateTween();
                
                // THE FIX: Use the stepped method for the shrink down as well!
                _scaleTween.TweenMethod(Callable.From<float>(ApplySteppedScale), 5.0f, 1.0f, fireball.OiledScaleTime)
                           .SetTrans(Tween.TransitionType.Linear);
                
                _scaleTween.Finished += () => 
                {
                    if (_returnState == "FireballClimb")
                    {
                        EmitSignal(SignalName.Transitioned, this, "FireballClimb");
                    }
                    else
                    {
                        if (fireball.IsOnFloor())
                            EmitSignal(SignalName.Transitioned, this, "FireballIdle");
                        else
                            EmitSignal(SignalName.Transitioned, this, "FireballFall");
                    }
                };
            }
        }
    }

    public override void Exit()
    {
        if (Enemy is Fireball fireball)
        {
            _scaleTween?.Kill();
            fireball.Scale = new Vector2(1, 1);
        }
    }
}