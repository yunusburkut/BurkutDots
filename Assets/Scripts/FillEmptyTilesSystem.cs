using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class FillEmptyTilesSystem : SystemBase
{
    protected override void OnUpdate()
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

    // Sütun bazlı işlem yap
    for (int col = 0; col < columns; col++)
    {
        int writeRow = 0; // Yeni pozisyon için yazma işaretçisi

        // Grid'i alt satırdan üst satıra tarayın
        for (int row = 0; row < rows; row++)
        {
            int index = row * columns + col;

            
            if (grid[index] == -2) // -2 engeller için özel bir işaret
            {
                writeRow = row + 1; // Engel üstüne yazma işlemi yapılmaz
                continue;
            }

            if (grid[index] != -1&&grid[index] != -2) // Dolu hücre
            {
                if (row != writeRow) // Kaydırılması gerekiyorsa
                {
                    int writeIndex = writeRow * columns + col;

                    // Grid ve GridEntities güncelle
                    grid[writeIndex] = grid[index];
                    grid[index] = -1;

                    gridEntities[writeIndex] = gridEntities[index];
                    gridEntities[index] = Entity.Null;

                    // Entity pozisyonunu güncelle
                    UpdateEntityPosition(gridEntities[writeIndex], col, writeRow);
                }
                writeRow++; // Yazma işaretçisini bir satır aşağı kaydır
            }
        }

        // Yeni bloklar oluştur ve üst satırdan başlat
        for (int row = writeRow; row < rows; row++)
        {
            int index = row * columns + col;
        
            if (grid[index] == -1) // Boş hücre
            {
                int newColor = UnityEngine.Random.Range(0, 6); // Rastgele renk
                grid[index] = newColor;
        
                Entity newTile = AddSpawnerTile(col, row, newColor);
                gridEntities[index] = newTile; // GridEntities dizisine yeni entity'yi ekle
            }
        }
    }
}


    private void UpdateEntityPosition(Entity entity, int col, int newRow)
    {
        if (entity == Entity.Null)
            return;

        var transform = EntityManager.GetComponentData<LocalTransform>(entity);

        // Hareket bileşeni ekle
        EntityManager.AddComponentData(entity, new MovingTileComponent
        {
            StartPosition = transform.Position,
            EndPosition = new float3(col, newRow, transform.Position.z),
            Duration = .5f,
            ElapsedTime = 0
        });

        // Hareket durumunu güncellemek için ek bileşen
        EntityManager.AddComponent<Moving>(entity);
    }

    private Entity AddSpawnerTile(int col, int row, int colorIndex)
    {
        // Başlangıç pozisyonu yukarıda başlat
        float startRow = 15;

        if (!SystemAPI.TryGetSingleton<TilePrefabComponent>(out var tilePrefabComponent))
        {
            Debug.LogError("TilePrefabComponent bulunamadı!");
            return Entity.Null;
        }

        Entity tilePrefab = tilePrefabComponent.PrefabEntity;

        // Yeni entity oluştur
        Entity newTile = EntityManager.Instantiate(tilePrefab);

        float3 startPosition = new float3(col, startRow, 0);
        float3 endPosition = new float3(col, row, 0);
// Normal taş oluştur
        
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
            Scale = .45f
        });

        EntityManager.SetComponentData(newTile, new TileData
        {
            ColorIndex = colorIndex,
        });

        // Düşüş animasyonu için hareket bileşeni ekle
        EntityManager.AddComponentData(newTile, new MovingTileComponent
        {
            StartPosition = startPosition,
            EndPosition = endPosition,
            Duration = .5f,
            ElapsedTime = 0
        });

        // Hareket durumunu takip etmek için ek bileşen
        EntityManager.AddComponent<Moving>(newTile);

        return newTile;
    }
}
