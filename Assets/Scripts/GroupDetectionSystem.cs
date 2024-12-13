using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using UnityEngine;

public partial class GroupDetectionSystem : SystemBase
{
    private SpriteArrayAuthoring colorSpriteManager;
    private int rows;
    private int columns;

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
    }

    // Sistem manuel olarak çalıştırıldığında bu fonksiyon çağrılır
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
        rows = boardState.Rows;
        columns = boardState.Columns;

        // Ziyaret edilen hücreleri işaretlemek için geçici bir NativeArray oluşturun
        NativeArray<bool> visited = new NativeArray<bool>(grid.Length, Allocator.Temp);

        // Grup tespiti ve sprite güncellemesi
        for (int index = 0; index < grid.Length; index++)
        {
            if (visited[index] || grid[index] < 0 || grid[index] == -2)
                continue; // Ziyaret edilen, boş veya engel hücreleri atla

            var group = new NativeList<int>(Allocator.Temp);
            FindGroup(index, grid[index], group, grid, visited);

            if (group.Length > 0) // Minimum grup boyutu kontrolü
            {
                Sprite selectedSprite = GetSpriteForGroupSize(group.Length, grid[index]);

                foreach (int cellIndex in group)
                {
                    int row = cellIndex / columns;
                    int col = cellIndex % columns;

                    if (grid[cellIndex] == -2) // Engel hücreleri atla
                        continue;

                    var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);

                    Entities
                        .WithAll<SpriteRenderer, LocalTransform>()
                        .ForEach((Entity entity, SpriteRenderer spriteRenderer, in LocalTransform transform) =>
                        {
                            if ((int)math.round(transform.Position.y) == row &&
                                (int)math.round(transform.Position.x) == col)
                            {
                                spriteRenderer.sprite = selectedSprite;
                                spriteRenderer.sortingOrder = row;
                            }
                        }).WithoutBurst().Run();

                    ecb.Playback(EntityManager);
                    ecb.Dispose();
                }
            }

            group.Dispose();
        }

        visited.Dispose();
    }

    private void FindGroup(int startIndex, int color, NativeList<int> group, NativeArray<int> grid, NativeArray<bool> visited)
    {
        NativeQueue<int> queue = new NativeQueue<int>(Allocator.Temp);
        queue.Enqueue(startIndex);

        while (queue.TryDequeue(out int currentIndex))
        {
            if (visited[currentIndex])
                continue;

            // Engel veya boş hücreleri atla
            if (grid[currentIndex] == -2 || grid[currentIndex] == -1)
                continue;

            visited[currentIndex] = true;
            group.Add(currentIndex);

            int row = currentIndex / columns;
            int col = currentIndex % columns;

            TryAddNeighbor(row - 1, col, color, queue, grid, visited);
            TryAddNeighbor(row + 1, col, color, queue, grid, visited);
            TryAddNeighbor(row, col - 1, color, queue, grid, visited);
            TryAddNeighbor(row, col + 1, color, queue, grid, visited);
        }

        queue.Dispose();
    }

    private void TryAddNeighbor(int row, int col, int color, NativeQueue<int> queue, NativeArray<int> grid, NativeArray<bool> visited)
    {
        if (row < 0 || row >= rows || col < 0 || col >= columns)
            return;

        int neighborIndex = row * columns + col;

        // Eğer hücre ziyaret edilmişse veya engelse işlem yapma
        if (!visited[neighborIndex] && grid[neighborIndex] == color && grid[neighborIndex] != -2)
        {
            queue.Enqueue(neighborIndex);
        }
    }

    private Sprite GetSpriteForGroupSize(int groupSize, int colorIndex)
    {
        foreach (var mapping in colorSpriteManager.mappings)
        {
            if (mapping.ColorID == colorIndex)
            {
                if (groupSize == 1 && mapping.Sprites.Length > 1)
                {
                    return mapping.Sprites[0];
                }
                else if (groupSize == 2 && mapping.Sprites.Length > 2)
                {
                    return mapping.Sprites[1];
                }
                else if (groupSize == 3 && mapping.Sprites.Length > 0)
                {
                    return mapping.Sprites[2];
                }
                else if (groupSize >= 4 && mapping.Sprites.Length > 0)
                {
                    return mapping.Sprites[3];
                }
            }
        }

        Debug.LogWarning($"Grup boyutu {groupSize} ve renk {colorIndex} için uygun sprite bulunamadı.");
        return null;
    }
}
