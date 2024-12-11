using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using UnityEngine;

public partial class GroupDetectionSystem : SystemBase
{
    private SpriteArrayAuthoring colorSpriteManager;
    private NativeArray<int> grid;
    private NativeArray<bool> visited;
    private int rows = 10;
    private int columns = 10;

    protected override void OnCreate()
    {
        int totalCells = rows * columns;
        grid = new NativeArray<int>(totalCells, Allocator.Persistent);
        visited = new NativeArray<bool>(totalCells, Allocator.Persistent);
        
    }

    protected override void OnDestroy()
    {
        if (grid.IsCreated) grid.Dispose();
        if (visited.IsCreated) visited.Dispose();
    }

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

        // Grid ve visited dizilerini sıfırla
        for (int i = 0; i < grid.Length; i++)
        {
            grid[i] = -1;
            visited[i] = false;
        }

        // Grid'i doldur
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
            }).WithoutBurst().Run();

        // Grup tespiti ve sprite güncellemesi
        for (int index = 0; index < grid.Length; index++)
        {
            if (visited[index] || grid[index] < 0)
                continue;

            var group = new NativeList<int>(Allocator.Temp);
            FindGroup(index, grid[index], group);

            if (group.Length > -1) // Minimum grup boyutu
            {
                Sprite selectedSprite = GetSpriteForGroupSize(group.Length, grid[index]);

                foreach (int cellIndex in group)
                {
                    int row = cellIndex / columns;
                    int col = cellIndex % columns;

                    Entities
                        .WithAbsent<MovingTileComponent>()
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
                }
            }

            group.Dispose();
        }
    }

    private void FindGroup(int startIndex, int color, NativeList<int> group)
    {
        NativeQueue<int> queue = new NativeQueue<int>(Allocator.Temp);
        queue.Enqueue(startIndex);

        while (queue.TryDequeue(out int currentIndex))
        {
            if (visited[currentIndex])
                continue;

            visited[currentIndex] = true;
            group.Add(currentIndex);

            int row = currentIndex / columns;
            int col = currentIndex % columns;

            TryAddNeighbor(row - 1, col, color, queue);
            TryAddNeighbor(row + 1, col, color, queue);
            TryAddNeighbor(row, col - 1, color, queue);
            TryAddNeighbor(row, col + 1, color, queue);
        }

        queue.Dispose();
    }

    private void TryAddNeighbor(int row, int col, int color, NativeQueue<int> queue)
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
