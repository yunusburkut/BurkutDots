using Unity.Entities;

public struct TileData : IComponentData
{
    public int ColorIndex; // 0-5 arasında renk
    public bool IsObstacle; // Engel olup olmadığını belirtir
    public int ObstacleHealth; // Engelin kalan canı (0 ise yok)
}