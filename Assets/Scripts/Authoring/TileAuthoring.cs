using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

// Tile Authoring Component
public class TileAuthoring : MonoBehaviour
{
    public int DefaultColorIndex = 0;
    public bool IsObstacle = false;
    public int ObstacleHealth = 0;
    public int2 GridPosition = new int2(0, 5);
}

public class TileBaker : Baker<TileAuthoring>
{
    public override void Bake(TileAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new TileDataComponent
        {
            ColorIndex = authoring.DefaultColorIndex,
            GridPosition = authoring.GridPosition
        });

    }
}