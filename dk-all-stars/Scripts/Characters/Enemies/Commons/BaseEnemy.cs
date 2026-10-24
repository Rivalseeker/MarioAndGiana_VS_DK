using Godot;

public partial class BaseEnemy : CharacterBody2D
{
    [Export] public float FallGravity = 1500.0f; // Standard gravity for enemies

    // Shared method for reading TileMap Custom Data via Physics
    public bool GetRaycastCustomData(RayCast2D ray, string dataName)
    {
        if (ray == null || !ray.IsColliding()) return false;

        if (ray.GetCollider() is TileMapLayer layer)
        {
            Vector2 hitPoint = ray.GetCollisionPoint() + (ray.TargetPosition.Normalized() * 2.0f);
            Vector2I cell = layer.LocalToMap(layer.ToLocal(hitPoint));
            TileData data = layer.GetCellTileData(cell);
            
            if (data != null)
            {
                Variant customData = data.GetCustomData(dataName);
                if (customData.VariantType == Variant.Type.Bool) return customData.AsBool();
            }
        }
        return false;
    }

    // Shared method for centering on a ladder
    public void CenterOnLadderX(RayCast2D ray)
    {
        if (ray != null && ray.IsColliding() && ray.GetCollider() is TileMapLayer layer)
        {
            Vector2 hitPoint = ray.GetCollisionPoint() + (ray.TargetPosition.Normalized() * 2.0f);
            Vector2I cell = layer.LocalToMap(layer.ToLocal(hitPoint));
            Vector2 localCenter = layer.MapToLocal(cell);
            float globalX = layer.ToGlobal(localCenter).X;
            
            // THE FIX: Round the resulting X coordinate to the nearest whole number!
            GlobalPosition = new Vector2(Mathf.Round(globalX), GlobalPosition.Y);
        }
    }
}