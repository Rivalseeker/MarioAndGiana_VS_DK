using Godot;

public partial class LevelHudControl : Control
{
    [ExportCategory("HUD Dimming")]
    [Export] private Area2D _obscureZone;
    [Export] private float _dimmedAlpha = 0.25f; 
    [Export] private float _fadeDuration = 0.2f; 

    private Tween _fadeTween;
    private int _entitiesInside = 0;

    public override void _Ready()
    {
        if (_obscureZone != null)
        {
            _obscureZone.BodyEntered += OnBodyEntered;
            _obscureZone.BodyExited += OnBodyExited;
        }
        else
        {
            GD.PrintErr("HudObscureZone is not assigned in LevelHudControl!");
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        _entitiesInside++;
        
        if (_entitiesInside == 1)
        {
            SetDimmed(true);
        }
    }

    private void OnBodyExited(Node2D body)
    {
        _entitiesInside--;
        
        if (_entitiesInside <= 0)
        {
            _entitiesInside = 0; 
            SetDimmed(false);
        }
    }

    private void SetDimmed(bool isDimmed)
    {
        _fadeTween?.Kill();
        _fadeTween = CreateTween();

        float targetAlpha = isDimmed ? _dimmedAlpha : 1.0f;
        float currentAlpha = Modulate.A;
        
        // THE FIX: Use TweenMethod to calculate the transition, but send the 
        // raw numbers to our custom "ApplySteppedAlpha" method below instead of the screen.
        _fadeTween.TweenMethod(Callable.From<float>(ApplySteppedAlpha), currentAlpha, targetAlpha, _fadeDuration)
                  .SetTrans(Tween.TransitionType.Linear); // Linear pacing feels best for retro steps
    }

    // NEW: Our custom method that forces the fade into retro 25% chunks
    private void ApplySteppedAlpha(float rawAlpha)
    {
        // The Math Trick: Multiply by 4, round to the nearest whole number, then divide by 4.
        // This instantly snaps ANY decimal into exactly 0.25, 0.50, 0.75, or 1.0!
        float steppedAlpha = Mathf.Round(rawAlpha * 4.0f) / 4.0f;
        
        // Safety catch to ensure it never drops below your desired minimum
        steppedAlpha = Mathf.Clamp(steppedAlpha, _dimmedAlpha, 1.0f);

        // Apply the newly chunked alpha value to the UI
        Color newModulate = Modulate;
        newModulate.A = steppedAlpha;
        Modulate = newModulate;
    }
}