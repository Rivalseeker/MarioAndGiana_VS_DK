using Godot;

public partial class BaseCollectible : Area2D
{
    [ExportCategory("Item Settings")]
    [Export] public string UniqueId = "Item_1"; 
    
    // NEW: Makes the script flexible in case a level has more or fewer items!
    [Export] public int TotalItemsInLevel = 3; 

    [ExportCategory("Node References")]
    [Export] public Sprite2D MainSprite; 
    [Export] public Label TallyLabel; // NEW: Reference to your Tally text node
    [Export] public AnimationPlayer AnimPlayer;
    [Export] public AudioStreamPlayer PickupSfx;

    public override void _Ready()
    {
        // THE FIX: Check both the permanent and temporary lists!
        if (SessionState.CollectedLevelItems.Contains(UniqueId) || SessionState.TempCollectedItems.Contains(UniqueId))
        {
            QueueFree();
            return;
        }

        BodyEntered += OnBodyEntered;

        if (AnimPlayer != null && AnimPlayer.HasAnimation("Idle")) AnimPlayer.Play("Idle");
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is BasePlayer player && !player.IsDead)
        {
            SetDeferred(Area2D.PropertyName.Monitoring, false);

            if (MainSprite != null) MainSprite.Hide();
            if (PickupSfx != null) PickupSfx.Play();

            // Add the item to the temporary pocket, NOT the permanent list!
            if (!SessionState.TempCollectedItems.Contains(UniqueId) && !SessionState.CollectedLevelItems.Contains(UniqueId))
            {
                SessionState.TempCollectedItems.Add(UniqueId);
            }
            
            if (TallyLabel != null) 
            {
                TallyLabel.Text = SessionState.GetCollectibleTallyText(TotalItemsInLevel);
            }

            if (AnimPlayer != null)
            {
                // Use the combined count for the animation logic
                int currentCount = SessionState.CollectedLevelItems.Count + SessionState.TempCollectedItems.Count;

                if (currentCount < TotalItemsInLevel)
                {
                    if (AnimPlayer.HasAnimation("Collected")) AnimPlayer.Play("Collected");
                }
                else
                {
                    if (AnimPlayer.HasAnimation("CollectedAll")) AnimPlayer.Play("CollectedAll");
                }
                
                AnimPlayer.AnimationFinished += (name) => QueueFree();
            }
            else
            {
                QueueFree();
            }
        }
    }
}