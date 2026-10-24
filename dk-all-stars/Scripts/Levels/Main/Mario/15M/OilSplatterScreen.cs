using Godot;

public partial class OilSplatterScreen : CanvasLayer
{
    [Export] public PackedScene ScreenSplatterScene;

    public void SpawnScreenSplatter()
    {
        if (ScreenSplatterScene == null) return;

        OilScreenSplatter splatter = ScreenSplatterScene.Instantiate<OilScreenSplatter>();
        AddChild(splatter);

        // Get the actual resolution of the game window
        Rect2 viewportRect = GetViewport().GetVisibleRect();

        // Because the sprite draws from -16 to +16 on the X axis, 
        // and -32 to 0 on the Y axis, we constrain the spawn coordinates:
        float minX = 16.0f;
        float maxX = viewportRect.Size.X - 16.0f;
        
        float minY = 32.0f;
        float maxY = viewportRect.Size.Y;

        // Pick random coordinates within the safe box
        float randomX = (float)GD.RandRange(minX, maxX);
        float randomY = (float)GD.RandRange(minY, maxY);

        splatter.Position = new Vector2(randomX, randomY);
    }
}