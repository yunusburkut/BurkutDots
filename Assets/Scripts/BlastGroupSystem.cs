using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using UnityEngine;



public partial class BlastGroupSystem : SystemBase
{
    private SpriteArrayAuthoring colorSpriteManager;

    protected override void OnStartRunning()
    {
        colorSpriteManager = Object.FindFirstObjectByType<SpriteArrayAuthoring>();

        if (colorSpriteManager == null || colorSpriteManager.mappings.Count == 0)
        {
            Debug.LogError("SpriteArrayAuthoring bulunamadı veya mappings boş!");
        }
    }

    protected override void OnUpdate()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        if (!SystemAPI.TryGetSingleton<BoardState>(out var boardState))
        {
            Debug.LogError("BoardState bulunamadı. BoardInitializationSystem'in çalıştığından emin olun.");
            return;
        }

        var grid = boardState.Grid;
        var gridEntities = boardState.GridEntities;
        int rows = boardState.Rows;
        int columns = boardState.Columns;

        // Fare pozisyonunu al ve grid pozisyonuna çevir
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        int2 gridPosition = new int2((int)math.floor(worldPos.x), (int)math.floor(worldPos.y));

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
                    int index = row * columns + col;

                    if (index >= 0 && index < grid.Length)
                    {
                        grid[index] = -1; // Patlayan hücreyi boş yap
                        gridEntities[index] = Entity.Null;
                    }
                }

                ecb.DestroyEntity(entity); // Patlayan entity'yi yok et
            }

            // Obstacle canını azalt ve yok et
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

        NativeHashSet<int2> visited = new NativeHashSet<int2>(10, Allocator.TempJob);
        int groupColor = -1;

        while (queue.TryDequeue(out int2 current))
        {
            if (visited.Contains(current))
                continue;

            visited.Add(current);

            int index = current.y * boardState.Columns + current.x;
            if (index < 0 || index >= boardState.Grid.Length || boardState.Grid[index] < 0)
                continue;

            if (groupColor == -1)
            {
                groupColor = boardState.Grid[index];
            }

            if (boardState.Grid[index] == groupColor)
            {
                groupEntities.Add(boardState.GridEntities[index]);

                queue.Enqueue(new int2(current.x + 1, current.y));
                queue.Enqueue(new int2(current.x - 1, current.y));
                queue.Enqueue(new int2(current.x, current.y + 1));
                queue.Enqueue(new int2(current.x, current.y - 1));
            }
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
        if (row < 0 || row >= rows || col < 0 || col >= columns)
            return;

        int index = row * columns + col;

        if (grid[index] == -2) // Obstacle kontrolü
        {
            var obstacleEntity = gridEntities[index];
            if (EntityManager.HasComponent<ObstacleData>(obstacleEntity))
            {
                var obstacleData = EntityManager.GetComponentData<ObstacleData>(obstacleEntity);

                // Canı azalt
                obstacleData.Health--;

                if (obstacleData.Health <= 0)
                {
                    grid[index] = -1; // Engel kaldırıldı
                    gridEntities[index] = Entity.Null;

                    ecb.DestroyEntity(obstacleEntity); // Obstacle'ı yok et
                }
                else if (obstacleData.Health <= 1)
                {
                    // Sprite güncelle
                    UpdateObstacleSprite(obstacleEntity);
                }

                // ObstacleData güncelle
                EntityManager.SetComponentData(obstacleEntity, obstacleData);
            }
        }
    }

    private void UpdateObstacleSprite(Entity obstacleEntity)
    {
        if (!EntityManager.HasComponent<SpriteRenderer>(obstacleEntity))
            return;

        var spriteRenderer = EntityManager.GetComponentObject<SpriteRenderer>(obstacleEntity);

        // Maksimum cana göre hangi sprite kullanılacak hesaplanır
        
        if (spriteRenderer != null)
        {
            Sprite newSprite = GetObstacleSprite();
            if (newSprite != null)
            {
                spriteRenderer.sprite = newSprite;
            }
        }
    }

    private Sprite GetObstacleSprite()
    {
        // SpriteArrayAuthoring veya başka bir mekanizmadan sprite seç
        if (colorSpriteManager != null && colorSpriteManager.mappings.Count > 0)
        {
            return colorSpriteManager.mappings[6].Sprites[1];
        }

        Debug.LogWarning("Obstacle için uygun sprite bulunamadı!");
        return null;
    }
}
