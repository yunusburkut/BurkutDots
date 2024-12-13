using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
public partial class FillEmptyTilesSystem : SystemBase
{
    private MapSettings mapSettings;
    protected override void OnUpdate()
    {
    }

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
    
    public void RunDetection()
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

            
            if (grid[index] == -2) // -2 obstacle 
            {
                writeRow = row + 1; //Obstacleların üzerine yazma işlemi yapılmasın diye +1 ekliyoruz row'a üstüne yazsın diye
                continue;
            }

            if (grid[index] != -1&&grid[index] != -2) 
            {
                if (row != writeRow) //Row'un kaydırılması gerekiyor mu check'i
                {
                    int writeIndex = writeRow * columns + col;
                    grid[writeIndex] = grid[index];
                    grid[index] = -1;

                    gridEntities[writeIndex] = gridEntities[index];
                    gridEntities[index] = Entity.Null;

                    UpdateEntityPosition(gridEntities[writeIndex], col, writeRow);
                }
                writeRow++; 
            }
        }
        for (int row = writeRow; row < rows; row++)
        {
            int index = row * columns + col;
        
            if (grid[index] == -1) // -1 boş hücre check'i
            {
                int newColor = UnityEngine.Random.Range(0, 6); 
                grid[index] = newColor;
        
                Entity newTile = AddSpawnerTile(col, row, newColor);
                gridEntities[index] = newTile;
            }
        }
    } 
    }

    private void UpdateEntityPosition(Entity entity, int col, int newRow)
    {
        if (entity == Entity.Null)
            return;

        var transform = EntityManager.GetComponentData<LocalTransform>(entity);

        // entity'nin hareket etmesini sağlayan component'ı ekliyoruz
        EntityManager.AddComponentData(entity, new MovingTileComponent
        {
            StartPosition = transform.Position,
            EndPosition = new float3(col, newRow, transform.Position.z),
            Duration = .5f,
            ElapsedTime = 0
        });

        // Diğer sistemlerde state belirtmek için component eklemeyi tercih ettim
        EntityManager.AddComponent<Moving>(entity);
    }

    private Entity AddSpawnerTile(int col, int row, int colorIndex)
    {
        float startRow = mapSettings.M+5;//girilen row sayısına göre hep 5 row üstünde spawnlıyor

        if (!SystemAPI.TryGetSingleton<TilePrefabComponent>(out var tilePrefabComponent))
        {
            Debug.LogError("TilePrefabComponent bulunamadı!");
            return Entity.Null;
        }

        Entity tilePrefab = tilePrefabComponent.PrefabEntity;
        Entity newTile = EntityManager.Instantiate(tilePrefab);
        float3 startPosition = new float3(col, startRow, 0);
        float3 endPosition = new float3(col, row, 0);
        SpriteArrayAuthoring colorSpriteManager = Object.FindFirstObjectByType<SpriteArrayAuthoring>();

        if (EntityManager.HasComponent<SpriteRenderer>(newTile))
        {
            var spriteRenderer = EntityManager.GetComponentObject<SpriteRenderer>(newTile);
            spriteRenderer.sprite = colorSpriteManager.mappings[colorIndex].Sprites[0];
            spriteRenderer.sortingOrder = row;
        }
        // Tile'ın başlangıç pozisyonunu ayarlıyoruz
        EntityManager.SetComponentData(newTile, new LocalTransform
        {
            Position = startPosition,
            Rotation = quaternion.identity,
            Scale = .45f
        });

        EntityManager.SetComponentData(newTile, new TileDataComponent
        {
            ColorIndex = colorIndex,
        });

        // Düşüş animasyonu için hareket bileşeni ekliyoruz
        EntityManager.AddComponentData(newTile, new MovingTileComponent
        {
            StartPosition = startPosition,
            EndPosition = endPosition,
            Duration = .5f,
            ElapsedTime = 0
        });
        EntityManager.AddComponent<Moving>(newTile);

        return newTile;
    }
}
