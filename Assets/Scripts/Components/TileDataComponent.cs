using Unity.Entities;
using Unity.Mathematics;

public struct TileDataComponent : IComponentData
{
    public int ColorIndex; 
    public int2 GridPosition; 
}