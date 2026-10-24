using Godot;

public partial class OilWellPump : Node2D
{
    [ExportCategory("Splatter Settings")]
    [Export] public PackedScene OilSplatterScene;
    [Export] public Marker2D EmissionPoint;
    [Export] public float SplatterGravity = 900.0f;

    // THE FIX 1: Add a reference to your new container node
    [Export] public Node2D SplatterContainer; 

    public void SpawnSplatters()
    {
        // Added SplatterContainer to the safety check
        if (OilSplatterScene == null || EmissionPoint == null || SplatterContainer == null) return;

        float startX = EmissionPoint.GlobalPosition.X;

        for (int i = 0; i < 8; i++)
        {
            SpawnSingleSplatter(startX - 288.0f, startX - 16.0f);
        }

        for (int i = 0; i < 8; i++)
        {
            SpawnSingleSplatter(startX + 16.0f, startX + 288.0f);
        }
    }

    private void SpawnSingleSplatter(float minX, float maxX)
    {
        OilSplatter splatter = OilSplatterScene.Instantiate<OilSplatter>();
        
        // THE FIX 2: Add the splatter directly to the container node!
        SplatterContainer.AddChild(splatter); 
        
        splatter.GlobalPosition = EmissionPoint.GlobalPosition;
        splatter.Gravity = SplatterGravity;

        float maxPeakY = EmissionPoint.GlobalPosition.Y - 216.0f;
        float minPeakY = EmissionPoint.GlobalPosition.Y - 32.0f;
        
        float peakY = (float)GD.RandRange(maxPeakY, minPeakY);
        
        float targetX = (float)GD.RandRange(minX, maxX);

        // ==========================================
        // KINEMATICS MATH
        // ==========================================
        float heightToTravel = EmissionPoint.GlobalPosition.Y - peakY;
        float velocityY = -Mathf.Sqrt(2 * SplatterGravity * heightToTravel);
        
        float timeToPeak = -velocityY / SplatterGravity;
        float totalAirTime = timeToPeak * 2.0f; 
        
        float distanceX = targetX - EmissionPoint.GlobalPosition.X;
        float velocityX = distanceX / totalAirTime;

        splatter.Velocity = new Vector2(velocityX, velocityY);
    }
}