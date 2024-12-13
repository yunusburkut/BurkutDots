using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using UnityEngine;

public partial class ClickValidationSystem : SystemBase
{
    private SpriteArrayAuthoring colorSpriteManager;

    protected override void OnUpdate()
    {
        
    }
    protected override void OnStartRunning()
    {
        colorSpriteManager = Object.FindFirstObjectByType<SpriteArrayAuthoring>();
        Enabled = false;

        if (colorSpriteManager == null || colorSpriteManager.mappings.Count == 0)
        {
            Debug.LogError("SpriteArrayAuthoring bulunamadı veya mappings boş!");
        }
        Enabled = false;
    }

    public void ProcessClick(Vector3 worldPos)
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

        // Dünya pozisyonunu grid pozisyonuna çevir
        int2 gridPosition = new int2((int)math.floor(worldPos.x), (int)math.floor(worldPos.y));

        // Pozisyonun grid içinde olup olmadığını kontrol et
        if (!IsWithinGrid(gridPosition, columns, rows))
        {
            Debug.Log("Tıklanan pozisyon grid dışında.");
            return;
        }

        // Grid indexini hesapla
        int index = gridPosition.y * columns + gridPosition.x;

        // Geçersiz hücre kontrolü
        if (grid[index] == -1 || grid[index] == -2)
        {
            Debug.Log("Boş veya engel tile'a tıklandı.");
            return;
        }

        // Entity'yi al
        Entity tileEntity = gridEntities[index];
        if (tileEntity == Entity.Null || EntityManager.HasComponent<Moving>(tileEntity))
        {
            Debug.Log("Geçersiz veya hareket eden tile'a tıklandı.");
            return;
        }

        Debug.Log($"Geçerli tile tıklandı: {gridPosition}");

        // Grup tespit et
        NativeList<Entity> groupEntities = FindGroupEntities(gridPosition, boardState);

        if (groupEntities.Length > 1)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            // Grubu patlat
            foreach (var entity in groupEntities)
            {
                if (EntityManager.HasComponent<LocalTransform>(entity))
                {
                    var transform = EntityManager.GetComponentData<LocalTransform>(entity);
                    int row = (int)math.round(transform.Position.y);
                    int col = (int)math.round(transform.Position.x);
                    int entityIndex = row * columns + col;

                    grid[entityIndex] = -1;
                    gridEntities[entityIndex] = Entity.Null;
                }

                ecb.DestroyEntity(entity);
            }

            // Obstacle canını azalt ve yönet
            DecreaseObstacleHealth(grid, gridEntities, groupEntities, rows, columns, ecb);

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        groupEntities.Dispose();
        Dependency.Complete();
    }

    private NativeList<Entity> FindGroupEntities(int2 startPos, BoardState boardState)
    {
        NativeList<Entity> groupEntities = new NativeList<Entity>(Allocator.TempJob);
        NativeQueue<int2> queue = new NativeQueue<int2>(Allocator.Temp);
        queue.Enqueue(startPos);

        NativeHashSet<int2> visited = new NativeHashSet<int2>(boardState.Grid.Length, Allocator.TempJob);
        int groupColor = boardState.Grid[startPos.y * boardState.Columns + startPos.x];

        while (queue.TryDequeue(out int2 current))
        {
            if (visited.Contains(current) || !IsWithinGrid(current, boardState.Columns, boardState.Rows))
                continue;

            visited.Add(current);

            int index = current.y * boardState.Columns + current.x;

            if (boardState.Grid[index] != groupColor)
                continue;

            groupEntities.Add(boardState.GridEntities[index]);

            // Komşuları sıraya ekle
            queue.Enqueue(new int2(current.x + 1, current.y));
            queue.Enqueue(new int2(current.x - 1, current.y));
            queue.Enqueue(new int2(current.x, current.y + 1));
            queue.Enqueue(new int2(current.x, current.y - 1));
        }

        visited.Dispose();
        queue.Dispose();

        return groupEntities;
    }

    private void DecreaseObstacleHealth(NativeArray<int> grid, NativeArray<Entity> gridEntities, NativeList<Entity> groupEntities, int rows, int columns, EntityCommandBuffer ecb)
    {
        foreach (var entity in groupEntities)
        {
            if (!EntityManager.HasComponent<LocalTransform>(entity))
                continue;

            var transform = EntityManager.GetComponentData<LocalTransform>(entity);
            int row = (int)math.round(transform.Position.y);
            int col = (int)math.round(transform.Position.x);

            // Komşuları kontrol et
            CheckAndDecreaseObstacle(row - 1, col, grid, gridEntities, rows, columns, ecb);
            CheckAndDecreaseObstacle(row + 1, col, grid, gridEntities, rows, columns, ecb);
            CheckAndDecreaseObstacle(row, col - 1, grid, gridEntities, rows, columns, ecb);
            CheckAndDecreaseObstacle(row, col + 1, grid, gridEntities, rows, columns, ecb);
        }
    }

    private void CheckAndDecreaseObstacle(int row, int col, NativeArray<int> grid, NativeArray<Entity> gridEntities, int rows, int columns, EntityCommandBuffer ecb)
    {
        if (!IsWithinGrid(new int2(col, row), columns, rows))
            return;

        int index = row * columns + col;

        if (grid[index] == -2) // Obstacle kontrolü
        {
            Entity obstacleEntity = gridEntities[index];
            if (EntityManager.HasComponent<ObstacleData>(obstacleEntity))
            {
                var obstacleData = EntityManager.GetComponentData<ObstacleData>(obstacleEntity);

                obstacleData.Health--;

                if (obstacleData.Health <= 0)
                {
                    grid[index] = -1;
                    gridEntities[index] = Entity.Null;
                    ecb.DestroyEntity(obstacleEntity);
                }
                else
                {
                    UpdateObstacleSprite(obstacleEntity);
                }

                EntityManager.SetComponentData(obstacleEntity, obstacleData);
            }
        }
    }

    private void UpdateObstacleSprite(Entity obstacleEntity)
    {
        if (!EntityManager.HasComponent<SpriteRenderer>(obstacleEntity))
            return;

        var spriteRenderer = EntityManager.GetComponentObject<SpriteRenderer>(obstacleEntity);
        Sprite newSprite = GetObstacleSprite();

        if (spriteRenderer != null && newSprite != null)
        {
            spriteRenderer.sprite = newSprite;
        }
    }

    private Sprite GetObstacleSprite()
    {
        if (colorSpriteManager != null && colorSpriteManager.mappings.Count > 0)
        {
            return colorSpriteManager.mappings[6].Sprites[1];
        }

        Debug.LogWarning("Obstacle için uygun sprite bulunamadı!");
        return null;
    }

    private bool IsWithinGrid(int2 pos, int columns, int rows)
    {
        return pos.x >= 0 && pos.x < columns && pos.y >= 0 && pos.y < rows;
    }
}
