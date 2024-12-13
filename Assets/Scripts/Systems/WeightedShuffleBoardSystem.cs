using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public partial class WeightedShuffleBoardSystem : SystemBase
{
    private SpriteArrayAuthoring colorSpriteManager;

    // Renk ağırlıkları (örnek değerler)
    private readonly float[] colorWeights = { 1.0f, 1.5f, 2.0f, 0.5f, 1.8f, 11f };
    private MapSettings mapSettings;

    protected override void OnStartRunning()
    {
        colorSpriteManager = Object.FindFirstObjectByType<SpriteArrayAuthoring>();

        if (colorSpriteManager == null || colorSpriteManager.mappings.Count == 0)
        {
            Debug.LogError("SpriteArrayAuthoring bulunamadı veya mappings boş!");
        }
        mapSettings = Object.FindObjectOfType<MapSettings>();
        if (!mapSettings)
        {
            Debug.LogError("MapSettings bileşeni sahnede bulunamadı!");
            Enabled = false;
            return;
        }
    }

    protected override void OnUpdate()
    {
    }

    public void ShuffleBoard()
    {
        if (!SystemAPI.TryGetSingleton<BoardState>(out var boardState))
        {
            Debug.LogError("BoardState bulunamadı. BoardInitializationSystem'in çalıştığından emin olun.");
            return;
        }

        var grid = boardState.Grid;
        var gridEntities = boardState.GridEntities;
        int rows = boardState.Rows;
        int columns = boardState.Columns;

        // Tüm renkleri ağırlıklara göre topla
        NativeList<int> weightedColors = new NativeList<int>(Allocator.Temp);

        for (int i = 0; i < grid.Length; i++)
        {
            if (grid[i] >= 0) // Boş ve engel hücreleri atla
            {
                int color = grid[i];
                int weight = Mathf.RoundToInt(mapSettings.ColorWeights[color] * 10);

                for (int j = 0; j < weight; j++)
                {
                    weightedColors.Add(color);
                }
            }
        }

        // Weighted list'i karıştır
        Random random = new Random((uint)UnityEngine.Random.Range(1, int.MaxValue));
        for (int i = 0; i < weightedColors.Length; i++)
        {
            int swapIndex = random.NextInt(0, weightedColors.Length);
            (weightedColors[i], weightedColors[swapIndex]) = (weightedColors[swapIndex], weightedColors[i]);
        }

        // Grid'de geçerli hücrelere karışık renkleri uygula
        int colorIndex = 0;
        for (int i = 0; i < grid.Length; i++)
        {
            if (grid[i] >= 0) // Sadece geçerli hücreler
            {
                grid[i] = weightedColors[colorIndex++];
            }
        }

        // Tile bileşenlerini ve sprite'ları güncelle
        for (int i = 0; i < grid.Length; i++)
        {
            if (gridEntities[i] != Entity.Null && grid[i] >= 0) // Sadece geçerli hücreler
            {
                var tileEntity = gridEntities[i];

                if (EntityManager.HasComponent<TileDataComponent>(tileEntity))
                {
                    var tileData = EntityManager.GetComponentData<TileDataComponent>(tileEntity);
                    tileData.ColorIndex = grid[i];
                    EntityManager.SetComponentData(tileEntity, tileData);

                    // Sprite güncellemesi
                    if (EntityManager.HasComponent<SpriteRenderer>(tileEntity))
                    {
                        var spriteRenderer = EntityManager.GetComponentObject<SpriteRenderer>(tileEntity);
                        spriteRenderer.sprite = colorSpriteManager.mappings[grid[i]].Sprites[0];
                    }
                }
            }
        }

        weightedColors.Dispose();

        Debug.Log("Board ağırlıklı olarak karıştırıldı!");
    }
}
