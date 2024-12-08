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
}

// Baking Script
public class TileBaker : Baker<TileAuthoring>
{
    public override void Bake(TileAuthoring authoring)
    {
        // Entity'ye TileData bileşeni ekle
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new TileData
        {
            ColorIndex = authoring.DefaultColorIndex,
            IsObstacle = authoring.IsObstacle,
            ObstacleHealth = authoring.ObstacleHealth
        });

        // Entity'ye Position bileşeni ekle
        AddComponent(entity, new Position
        {
            GridPosition = new int2(0, 5) // Başlangıçta sıfır pozisyon
        });
    }
}