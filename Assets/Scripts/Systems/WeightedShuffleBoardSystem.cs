using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public partial class WeightedShuffleBoardSystem : SystemBase
{
    private SpriteArrayAuthoring colorSpriteManager;
    private MapSettings mapSettings;

    protected override void OnStartRunning()
    {
        colorSpriteManager = Object.FindFirstObjectByType<SpriteArrayAuthoring>();

        if (colorSpriteManager == null || colorSpriteManager.mappings.Count == 0)
        {
            Debug.LogError("SpriteArrayAuthoring bulunamadı veya mappings boş!");
        }

        mapSettings = Object.FindFirstObjectByType<MapSettings>();
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

        // Önce patlatılabilir bir grup olup olmadığını kontrol et bunun checkkini group detection'da
        // if (BlastableArrayCount <=0) ile yapıyoruz ama double check
     
        if (HasBlastableGroup(grid, rows, columns))
        {
            Debug.Log("Board'da zaten patlatılabilir bir grup var, shuffle gerekmez.");
            return;
        }

        Debug.Log("Board'da patlatılabilir grup bulunamadı. Karıştırılıyor...");

        // Tüm hücre renklerini topla
        NativeList<int> availableColors = new NativeList<int>(Allocator.Temp);

        for (int i = 0; i < grid.Length; i++)
        {
            if (grid[i] >= 0) // Boş ve obstacle hücreleri atla
            {
                availableColors.Add(grid[i]);
            }
        }
        Random random = new Random((uint)UnityEngine.Random.Range(1, int.MaxValue));
        for (int i = 0; i < availableColors.Length; i++)
        {
            int swapIndex = random.NextInt(0, availableColors.Length);
            (availableColors[i], availableColors[swapIndex]) = (availableColors[swapIndex], availableColors[i]);
        }
        //Grid'i karışık renklerle güncelliiyoruz 
        int colorIndex = 0;
        for (int i = 0; i < grid.Length; i++)
        {
            if (grid[i] >= 0)
            {
                grid[i] = availableColors[colorIndex++];
            }
        }
        UpdateTiles(grid, gridEntities);
        availableColors.Dispose();
        Debug.Log("Board başarıyla karıştırıldı!");
    }

    private void UpdateTiles(NativeArray<int> grid, NativeArray<Entity> gridEntities)
    {
        for (int i = 0; i < grid.Length; i++)
        {
            if (gridEntities[i] != Entity.Null && grid[i] >= 0) 
            {
                var tileEntity = gridEntities[i];

                if (EntityManager.HasComponent<TileDataComponent>(tileEntity))
                {
                    var tileData = EntityManager.GetComponentData<TileDataComponent>(tileEntity);
                    tileData.ColorIndex = grid[i];
                    EntityManager.SetComponentData(tileEntity, tileData);
                    
                    if (EntityManager.HasComponent<SpriteRenderer>(tileEntity))
                    {
                        var spriteRenderer = EntityManager.GetComponentObject<SpriteRenderer>(tileEntity);
                        spriteRenderer.sprite = colorSpriteManager.mappings[grid[i]].Sprites[0];
                    }
                }
            }
        }
    }

    private bool HasBlastableGroup(NativeArray<int> grid, int rows, int columns)
    {
        NativeArray<bool> visited = new NativeArray<bool>(grid.Length, Allocator.Temp);

        for (int i = 0; i < grid.Length; i++)
        {
            if (grid[i] >= 0 && !visited[i])
            {
                if (FindGroup(i, grid, rows, columns, visited).Length > 1)
                {
                    visited.Dispose();
                    return true; // Patlatılabilir bir grup bulundugu için ture
                }
            }
        }

        visited.Dispose();
        return false; // Patlatılabilir bir grup bulunamadıgı için false dönüyo
    }

    private NativeList<int> FindGroup(int startIndex, NativeArray<int> grid, int rows, int columns, NativeArray<bool> visited)
    {
        NativeList<int> group = new NativeList<int>(Allocator.Temp);
        NativeQueue<int> queue = new NativeQueue<int>(Allocator.Temp);

        queue.Enqueue(startIndex);
        int startColor = grid[startIndex];

        while (queue.TryDequeue(out int currentIndex))
        {
            if (visited[currentIndex] || grid[currentIndex] != startColor)
                continue;

            visited[currentIndex] = true;
            group.Add(currentIndex);

            int row = currentIndex / columns;
            int col = currentIndex % columns;

            TryAddNeighbor(row - 1, col, startColor, grid, rows, columns, queue);
            TryAddNeighbor(row + 1, col, startColor, grid, rows, columns, queue);
            TryAddNeighbor(row, col - 1, startColor, grid, rows, columns, queue);
            TryAddNeighbor(row, col + 1, startColor, grid, rows, columns, queue);
        }

        queue.Dispose();
        return group;
    }

    private void TryAddNeighbor(int row, int col, int color, NativeArray<int> grid, int rows, int columns, NativeQueue<int> queue)
    {
        if (row < 0 || row >= rows || col < 0 || col >= columns)
            return;

        int index = row * columns + col;
        if (grid[index] == color)
        {
            queue.Enqueue(index);
        }
    }
}
