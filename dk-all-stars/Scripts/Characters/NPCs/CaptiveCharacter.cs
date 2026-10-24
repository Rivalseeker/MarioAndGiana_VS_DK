using Godot;

public partial class CaptiveCharacter : Node2D
{
    [ExportCategory("Captive Settings")]
    // Creates a dropdown in the Inspector (-1 = Left, 1 = Right)
    [Export(PropertyHint.Enum, "Left:-1,Right:1")]
    public int FacingDirection = -1;

    [ExportCategory("Node References")]
    [Export] public Sprite2D CharacterSprite;
    [Export] public Sprite2D HelpSprite;
    [Export] public AnimationPlayer CaptiveAnim;

    public override void _Ready()
    {
        // Auto-fetch nodes if not assigned in the Inspector
        if (CaptiveAnim == null) CaptiveAnim = GetNodeOrNull<AnimationPlayer>("CaptiveAnim");
        if (CharacterSprite == null) CharacterSprite = GetNodeOrNull<Sprite2D>("CharacterSprite");
        if (HelpSprite == null) HelpSprite = GetNodeOrNull<Sprite2D>("HelpSprite");

        // Apply initial facing direction to the sprite
        UpdateFacingDirection(FacingDirection);

        // Start the scene by playing the correct looping Help animation
        PlayHelpAnimation();
    }

    public void UpdateFacingDirection(int direction)
    {
        FacingDirection = direction;

        if (CharacterSprite != null)
        {
            // Assuming your default sprite faces Right. 
            // If the raw image faces Left by default, change this to: FacingDirection > 0
            CharacterSprite.FlipH = FacingDirection < 0; 
        }
    }

    // ==========================================
    // ANIMATION TRIGGERS
    // ==========================================

    public void PlayHelpAnimation()
    {
        string targetAnim = FacingDirection == -1 ? "HelpLeft" : "HelpRight";
        PlayAnimation(targetAnim);
    }

    public void PlaySavedAnimation()
    {
        string targetAnim = FacingDirection == -1 ? "SavedLeft" : "SavedRight";
        PlayAnimation(targetAnim);
    }

    public void PlayTakenAwayAnimation()
    {
        // Direction-agnostic animation for when DK grabs her
        PlayAnimation("TakenAway");
    }

    private void PlayAnimation(string animName)
    {
        if (CaptiveAnim != null && CaptiveAnim.HasAnimation(animName))
        {
            CaptiveAnim.Play(animName);
        }
        else
        {
            GD.PrintErr($"CaptiveAnim is missing animation: {animName}");
        }
    }
}