using Unity.Entities;
using Unity.Mathematics;

public struct Position : IComponentData
{
    public int2 GridPosition; // Tile'ın ızgara üzerindeki x, y konumu
}