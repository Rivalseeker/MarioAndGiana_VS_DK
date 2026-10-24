using Godot;

public partial class OilDrum : Area2D
{
    [ExportCategory("Nodes")]
    [Export] public Sprite2D FireSprite;
    [Export] public AnimationPlayer OilFireAnim;
    
    [ExportCategory("Spawning")]
    [Export] public PackedScene FireballScene;
    [Export] public float LeapHorizontalSpeed = 40.0f; // Control horizontal push independently
    [Export] public float SpawnVerticalOffset = 32.0f;

    public override void _Ready()
    {
        if (FireSprite != null) FireSprite.Visible = false;
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Barrel barrel)
        {
            bool wasFlammable = barrel.IsFlammable;
            barrel.QueueFree();

            if (wasFlammable)
            {
                if (FireSprite != null) FireSprite.Visible = true;
                if (OilFireAnim != null) OilFireAnim.Play("Ignited");
            }
        }
    }

    public void AnimEvent_SpawnFireball()
    {
        if (FireballScene == null) return;

        Node2D fireball = FireballScene.Instantiate<Node2D>();
        GetParent().AddChild(fireball);
        fireball.GlobalPosition = GlobalPosition + new Vector2(0, -SpawnVerticalOffset); 

        if (fireball is Fireball enemyFireball)
        {
            // Pass the horizontal speed directly into the memory variable
            enemyFireball.SpawnLeapVelocity = new Vector2(LeapHorizontalSpeed, 0f);
        }

        if (fireball.GetNodeOrNull<EnemyStateMachine>("StateMachine") is EnemyStateMachine sm)
        {
            sm.ForceTransition("FireballSpawn");
        }
    }

    public void AnimEvent_PlayBurning()
    {
        OilFireAnim?.Play("Burning");
    }
}