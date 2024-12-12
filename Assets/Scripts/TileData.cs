using Unity.Entities;
using Unity.Mathematics;

public struct TileData : IComponentData
{
    public int ColorIndex; 
    public bool IsObstacle; 
    public int ObstacleHealth; 
    public int2 GridPosition; 
}