using Unity.Entities;
using Unity.Mathematics;

public struct MovingTileComponent : IComponentData
{
    public float3 StartPosition;
    public float3 EndPosition;
    public float Duration;
    public float ElapsedTime;
}