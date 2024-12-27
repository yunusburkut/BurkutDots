using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
public partial class FillEmptyTilesSystem : SystemBase
{
    private MapSettings mapSettings;

    protected override void OnUpdate() { }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        Enabled = false;
        mapSettings = Object.FindFirstObjectByType<MapSettings>();
        if (!mapSettings)
        {
            Debug.LogError("MapSettings bileşeni sahnede bulunamadı!");
            Enabled = false;
            return;
        }
    }

    public void RunFill()
    {
        // BoardState'e erişim
        if (!SystemAPI.TryGetSingleton<BoardState>(out var boardState))
        {
            Debug.LogError("BoardState bulunamadı. BoardInitializationSystem'in çalıştığından emin olun.");
            return;
        }

        var grid = boardState.Grid;
        var gridEntities = boardState.GridEntities;
        int rows = boardState.Rows;
        int columns = boardState.Columns;

        // Sütun bazlı kaydırma ve doldurma işlemi
        for (int col = 0; col < columns; col++)
        {
            int writeRow = 0; // Yazma konumunu takip eder
            int spawnRowOffset = mapSettings.M + 5; // Yeni Tile'lar üstte spawnlanır

            for (int row = 0; row < rows; row++)
            {
                int index = row * columns + col;

                if (grid[index] == -2) // Engel kontrolü
                {
                    writeRow = row + 1; // Engel üstüne yazılmaz
                    continue;
                }

                if (grid[index] != -1) // Boş olmayan hücreler
                {
                    if (row != writeRow) // Tile kaydırılacaksa
                    {
                        int writeIndex = writeRow * columns + col;

                        // Hücre değerlerini kaydır
                        grid[writeIndex] = grid[index];
                        grid[index] = -1;

                        // Entity değerlerini kaydır
                        gridEntities[writeIndex] = gridEntities[index];
                        gridEntities[index] = Entity.Null;

                        // Tile'ın pozisyonunu güncelle
                        UpdateEntityPosition(gridEntities[writeIndex], col, writeRow);
                    }

                    writeRow++;
                }
            }

            // Kalan boş hücreleri yeni Tile'larla doldur
            for (int row = writeRow; row < rows; row++)
            {
                int index = row * columns + col;

                if (grid[index] == -1) // Boş hücre
                {
                    int newColor = UnityEngine.Random.Range(0, 6); // Rastgele bir renk belirle
                    grid[index] = newColor;

                    // Yeni bir Tile oluştur
                    Entity newTile = AddSpawnerTile(col, row, newColor, spawnRowOffset--);
                    gridEntities[index] = newTile;
                }
            }
        }
        // Grup algılama işlemini tetikleme
    }
    
    private void UpdateEntityPosition(Entity entity, int col, int newRow)
    {
        if (entity == Entity.Null)
            return;

        var transform = EntityManager.GetComponentData<LocalTransform>(entity);

        // Entity'nin hareket etmesini sağlayan bileşeni ekliyoruz
        EntityManager.AddComponentData(entity, new MovingTileComponent
        {
            StartPosition = transform.Position,
            EndPosition = new float3(col, newRow, transform.Position.z),
            Duration = 0.5f,
            ElapsedTime = 0
        });

        // State belirtmek için hareket eden bir bileşen ekliyoruz
        EntityManager.AddComponent<Moving>(entity);
    }

    private Entity AddSpawnerTile(int col, int row, int colorIndex, float spawnRowOffset)
    {
        if (!SystemAPI.TryGetSingleton<TilePrefabComponent>(out var tilePrefabComponent))
        {
            Debug.LogError("TilePrefabComponent bulunamadı!");
            return Entity.Null;
        }

        Entity tilePrefab = tilePrefabComponent.PrefabEntity;
        Entity newTile = EntityManager.Instantiate(tilePrefab);
        float3 startPosition = new float3(col, spawnRowOffset, 0);
        float3 endPosition = new float3(col, row, 0);
        SpriteArrayAuthoring colorSpriteManager = Object.FindFirstObjectByType<SpriteArrayAuthoring>();

        if (EntityManager.HasComponent<SpriteRenderer>(newTile))
        {
            var spriteRenderer = EntityManager.GetComponentObject<SpriteRenderer>(newTile);
            spriteRenderer.sprite = colorSpriteManager.mappings[colorIndex].Sprites[0];
            spriteRenderer.sortingOrder = row;
        }

        // Tile'ın başlangıç pozisyonunu ayarla
        EntityManager.SetComponentData(newTile, new LocalTransform
        {
            Position = startPosition,
            Rotation = quaternion.identity,
            Scale = 0.45f
        });

        EntityManager.SetComponentData(newTile, new TileDataComponent
        {
            ColorIndex = colorIndex,
        });

        // Düşüş animasyonu için hareket bileşeni ekle
        EntityManager.AddComponentData(newTile, new MovingTileComponent
        {
            StartPosition = startPosition,
            EndPosition = endPosition,
            Duration = 0.5f,
            ElapsedTime = 0
        });
        EntityManager.AddComponent<Moving>(newTile);

        return newTile;
    }
}
