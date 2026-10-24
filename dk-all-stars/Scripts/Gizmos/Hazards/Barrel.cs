using Godot;
using System;

public partial class Barrel : BaseEnemy
{
    [ExportCategory("Movement Stats")]
    [Export] public float RollSpeed = 50.0f;
    [Export] public float MaxFallSpeed = 100.0f; 
    [Export] public float FallFriction = 200.0f; 

    [ExportCategory("Bounce Settings")] // NEW
    [Export] public float MinimumFallTimeToBounce = 0.15f; // Seconds in freefall before a bounce is triggered
    [Export] public float BounceVelocity = 50.0f; // Upward force of the bounce

    [Export] public float MaxStepHeight = 4.1f;

    [ExportCategory("Ladder Stats")]
    [Export] public float LadderSpeed = 60.0f;
    [Export(PropertyHint.Range, "0.0, 1.0")] public float LadderDropChance = 0.5f; // 50% chance to drop

    // Add these alongside your other [ExportCategory] groups in Barrel.cs
    [ExportCategory("Wild Stats")]
    [Export] public float WildHorizontalSpeed = 0.0f; // Set to 0 if it should fall straight down
    [Export] public float WildBounceVelocity = 60.0f; // How high it pops up before falling through
    [Export] public float WildPassThroughTime = 0.3f; // How long it ignores collision

    // The direction DK will eventually dictate (1 for right, -1 for left, 0 for straight down)
    public int WildDirection { get; set; } = 0; 
    
    // Store the original collision mask so we can restore it accurately
    public uint OriginalCollisionMask { get; set; }

    // Replace your old BarrelSprite reference with these two:
    [ExportCategory("Nodes")]
    [Export] public Sprite2D NormalBarrelSprite;
    [Export] public Sprite2D FlammableBarrelSprite;
    [Export] public AnimationPlayer BarrelAnim;
    [Export] public Area2D Hitbox;
    // [Export] public Area2D Hurtbox;

    [ExportCategory("RayCasts")] // (Update your existing category)
    [Export] public RayCast2D StepDetector;
    [Export] public RayCast2D LedgeDetectorLeft;
    [Export] public RayCast2D LedgeDetectorRight;

    // NEW: Property to track its type for later use (like spawning fireballs!)
    // UPDATE: Remove the "private set" so DK can assign it before AddChild()
    public bool IsFlammable = false;

    public int RollDirection { get; set; } = 1;

    // NEW: Handoff flag to tell the Roll state if it should preserve its momentum
    public bool KeepDirectionOnNextRoll { get; set; } = false;

    // Keeps track of the direction area the barrel is currently overlapping
    public BarrelDirectionArea CurrentDirectionArea { get; set; } = null;

    // Add this anywhere in Barrel.cs to store the ladder's exact X position
    public float TargetLedgeX { get; set; } = 0.0f;

    public override void _Ready()
    {
        FloorSnapLength = MaxStepHeight;
        OriginalCollisionMask = CollisionMask; // NEW: Save default collision layers

        // NEW: Enforce the proper sprite visibility the exact millisecond 
        // the barrel enters the scene tree and resolves its nodes!
        SetFlammable(IsFlammable);
        // GD.Print(IsFlammable);

        if (Hitbox != null)
        {
            Hitbox.AreaEntered += OnPlayerHurtAreaEntered;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        MoveAndSlide();

        // // NEW: Check if the barrel collided with a wall and reverse direction
        // if (IsOnWall())
        // {
        //     RollDirection *= -1;
        // }

        // Dynamically flip the RayCast to match the roll direction
        if (StepDetector != null && RollDirection != 0)
        {
            StepDetector.TargetPosition = new Vector2(Mathf.Abs(StepDetector.TargetPosition.X) * RollDirection, 0);
        }

        HandleStepUp();
    }

    // NEW: Helper method to toggle environment collision
    public void SetCollisionActive(bool active)
    {
        CollisionMask = active ? OriginalCollisionMask : 0;
    }

    private void OnPlayerHurtAreaEntered(Area2D area)
    {
        if (area.Name == "PlayerHurtBox")
        {
            BasePlayer targetPlayer = area.GetParent() as BasePlayer;

            if (!targetPlayer.IsDead)
            {
                targetPlayer.Die();
            }
        }
    }

    private void HandleStepUp()
    {
        if (!IsOnFloor() || StepDetector == null || !StepDetector.IsColliding()) return;

        if (RollDirection == 0) return;

        Vector2 forwardStep = new Vector2(RollDirection * 2.0f, 0);
        Vector2 upStep = new Vector2(0, -MaxStepHeight);

        if (!TestMove(GlobalTransform, upStep))
        {
            Transform2D raisedTransform = GlobalTransform.Translated(upStep);
            
            if (!TestMove(raisedTransform, forwardStep))
            {
                Transform2D raisedForwardTransform = raisedTransform.Translated(forwardStep);
                KinematicCollision2D collision = new KinematicCollision2D();
                
                if (TestMove(raisedForwardTransform, new Vector2(0, MaxStepHeight), collision))
                {
                    float stepLift = MaxStepHeight - collision.GetTravel().Y;
                    GlobalPosition += new Vector2(0, -stepLift);
                    Velocity = new Vector2(RollDirection * RollSpeed, Velocity.Y);
                }
            }
        }
    }

    // Replace your existing IsDetectingLedge method with this updated version:
    public bool IsDetectingLedge(RayCast2D ray)
    {
        if (ray != null && ray.IsColliding())
        {
            if (ray.GetCollider() is TileMapLayer tileMapLayer)
            {
                Vector2 point = ray.GetCollisionPoint() + new Vector2(0, 2.0f);
                
                Vector2I mapPos = tileMapLayer.LocalToMap(tileMapLayer.ToLocal(point));
                TileData tileData = tileMapLayer.GetCellTileData(mapPos);
                
                if (tileData != null)
                {
                    Variant isLedge = tileData.GetCustomData("IsLedge");
                    if (isLedge.VariantType == Variant.Type.Bool && isLedge.AsBool())
                    {
                        // NEW: Calculate the exact global center of this specific tile
                        Vector2 localCenter = tileMapLayer.MapToLocal(mapPos);
                        Vector2 globalCenter = tileMapLayer.ToGlobal(localCenter);
                        
                        // Store the X coordinate so the Ladder state can use it
                        TargetLedgeX = globalCenter.X;
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public void SetFlammable(bool isFlammable)
    {
        IsFlammable = isFlammable;
        if (NormalBarrelSprite != null && FlammableBarrelSprite != null) 
        {
            if (!IsFlammable)
            {
                NormalBarrelSprite.Visible = true;
                FlammableBarrelSprite.Visible = false;
            }
            else
            {
                NormalBarrelSprite.Visible = false;
                FlammableBarrelSprite.Visible = true;
            }
        }
        
    }

    // UPDATE: Make sure DK's animation track updates the frame for both sprites!
    public void SetFrame(int frame)
    {
        if (NormalBarrelSprite != null) NormalBarrelSprite.Frame = frame;
        // if (FlammableBarrelSprite != null) FlammableBarrelSprite.Frame = frame;

        // if (!IsFlammable)
        // {
        //     NormalBarrelSprite.Visible = true;
        //     FlammableBarrelSprite.Visible = false;
        // }
        // else
        // {
        //     NormalBarrelSprite.Visible = false;
        //     FlammableBarrelSprite.Visible = true;
        // }
    }

    public void SetFlammableFrame(int frame)
    {
        if (FlammableBarrelSprite != null) FlammableBarrelSprite.Frame = frame;
    }

    public void StopAnimation()
    {
        BarrelAnim?.Stop();
    }

    public void PlayAnimation(string animName)
    {
        BarrelAnim?.Play(animName);
    }
}