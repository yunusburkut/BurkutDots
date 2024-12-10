using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using UnityEngine;
using Unity.Jobs;

public partial class GroupDetectionSystem : SystemBase
{
    private SpriteArrayAuthoring colorSpriteManager;
    private bool hasRun = false;

    protected override void OnStartRunning()
    {
        colorSpriteManager = Object.FindObjectOfType<SpriteArrayAuthoring>();

        if (colorSpriteManager == null || colorSpriteManager.mappings.Count == 0)
        {
            Debug.LogError("SpriteArrayAuthoring bulunamadı veya mappings boş!");
        }
    }

    protected override void OnUpdate()
    {
        if (colorSpriteManager == null || colorSpriteManager.mappings.Count == 0) return;

        hasRun = true;

        int rows = 10;
        int columns = 10;
        int totalCells = rows * columns;

        NativeArray<int> grid = new NativeArray<int>(totalCells, Allocator.TempJob);
        NativeArray<bool> visited = new NativeArray<bool>(totalCells, Allocator.TempJob);

        Entities
            .WithAll<TileData, LocalTransform>()
            .ForEach((Entity entity, in TileData tileData, in LocalTransform transform) =>
            {
                int row = (int)math.round(transform.Position.y);
                int col = (int)math.round(transform.Position.x);
                int index = row * columns + col;

                if (index >= 0 && index < grid.Length)
                {
                    grid[index] = tileData.ColorIndex;
                }
            }).Run();

        for (int index = 0; index < totalCells; index++)
        {
            if (visited[index] || grid[index] < 0)
                continue;

            NativeList<int> group = new NativeList<int>(Allocator.Temp);
            int currentColor = grid[index];

            FindGroup(index, rows, columns, grid, visited, currentColor, group);

            if (group.Length > 0)
            {
                Sprite selectedSprite = GetSpriteForGroupSize(group.Length, currentColor);

                Entities
                    .WithAll<SpriteRenderer, LocalTransform>()
                    .ForEach((Entity entity, SpriteRenderer spriteRenderer, in LocalTransform transform) =>
                    {
                        int row = (int)math.round(transform.Position.y);
                        int col = (int)math.round(transform.Position.x);
                        int cellIndex = row * columns + col;

                        if (group.Contains(cellIndex))
                        {
                            spriteRenderer.sprite = selectedSprite;
                        }
                    }).WithoutBurst().Run();
            }

            group.Dispose();
        }

        grid.Dispose();
        visited.Dispose();
    }

    private void FindGroup(
        int index,
        int rows,
        int columns,
        NativeArray<int> grid,
        NativeArray<bool> visited,
        int color,
        NativeList<int> group)
    {
        NativeQueue<int> queue = new NativeQueue<int>(Allocator.Temp);
        queue.Enqueue(index);

        while (queue.TryDequeue(out int currentIndex))
        {
            if (visited[currentIndex])
                continue;

            visited[currentIndex] = true;
            group.Add(currentIndex);

            int row = currentIndex / columns;
            int col = currentIndex % columns;

            TryAddNeighbor(row - 1, col, rows, columns, grid, visited, color, queue);
            TryAddNeighbor(row + 1, col, rows, columns, grid, visited, color, queue);
            TryAddNeighbor(row, col - 1, rows, columns, grid, visited, color, queue);
            TryAddNeighbor(row, col + 1, rows, columns, grid, visited, color, queue);
        }

        queue.Dispose();
    }

    private void TryAddNeighbor(
        int row,
        int col,
        int rows,
        int columns,
        NativeArray<int> grid,
        NativeArray<bool> visited,
        int color,
        NativeQueue<int> queue)
    {
        if (row < 0 || row >= rows || col < 0 || col >= columns)
            return;

        int neighborIndex = row * columns + col;

        if (!visited[neighborIndex] && grid[neighborIndex] == color)
        {
            queue.Enqueue(neighborIndex);
        }
    }

    private Sprite GetSpriteForGroupSize(int groupSize, int colorIndex)
    {
        // Mapping'den ColorID'ye uygun sprite'ı bul
        foreach (var mapping in colorSpriteManager.mappings)
        {
            if (mapping.ColorID == colorIndex)
            {
                if (groupSize == 1 && mapping.Sprites.Length > 1)
                {
                    return mapping.Sprites[0]; // Grup boyutu 3 için 2. sprite
                }
                else if (groupSize == 2 && mapping.Sprites.Length > 2)
                {
                    return mapping.Sprites[1]; // Grup boyutu 4 veya daha büyük için 3. sprite
                }
                else if (groupSize == 3 && mapping.Sprites.Length > 0)
                {
                    return mapping.Sprites[2]; // Varsayılan (grup boyutu 2'den büyük)
                }
                else if (groupSize >= 4 && mapping.Sprites.Length > 0)
                {
                    return mapping.Sprites[3]; // Varsayılan (grup boyutu 2'den büyük)
                }
            }
        }

        Debug.LogWarning($"Grup boyutu {groupSize} ve renk {colorIndex} için uygun sprite bulunamadı.");
        return null;
    }
}
