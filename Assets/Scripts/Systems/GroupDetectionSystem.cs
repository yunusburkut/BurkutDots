using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

[BurstCompile]
public partial class GroupDetectionSystem : SystemBase
{
    protected override void OnUpdate() { }

    public void RunDetection()
    {
        // BoardState'e erişim
        if (!SystemAPI.TryGetSingleton<BoardState>(out var boardState))
        {
            Debug.LogError("BoardState bulunamadı. BoardInitializationSystem'in çalıştığından emin olun.");
            return;
        }

        var grid = boardState.Grid;
        var groupIDGrid = boardState.GroupIDGrid;
        var gridEntities = boardState.GridEntities;
        int rows = boardState.Rows;
        int columns = boardState.Columns;

        NativeArray<bool> visited = new NativeArray<bool>(grid.Length, Allocator.Temp);
        int currentGroupID = 0;

        for (int index = 0; index < grid.Length; index++)
        {
            if (visited[index] || grid[index] < 0) // Engel veya boş hücre
                continue;

            currentGroupID++;
            NativeList<int> group = new NativeList<int>(Allocator.Temp);

            // Grup algılama işlemi
            FindGroup(index, grid[index], currentGroupID, group, grid, visited, groupIDGrid, rows, columns);

            if (group.Length <= 1)
            {
                foreach (var cellIndex in group)
                {
                    groupIDGrid[cellIndex] = -1; // Grup oluşturamayan hücreler
                }
            }
            else
            {
                foreach (var cellIndex in group)
                {
                    groupIDGrid[cellIndex] = currentGroupID; // Yeni GroupID ata
                }
            }

            group.Dispose();
        }

        visited.Dispose();

        // SortingOrder güncelleme
        UpdateSortingOrder(gridEntities, rows, columns);
    }

    private void FindGroup(int startIndex, int color, int groupID, NativeList<int> group, NativeArray<int> grid, NativeArray<bool> visited, NativeArray<int> groupIDGrid, int rows, int columns)
    {
        NativeQueue<int> queue = new NativeQueue<int>(Allocator.Temp);
        queue.Enqueue(startIndex);

        while (queue.TryDequeue(out int currentIndex))
        {
            if (visited[currentIndex] || grid[currentIndex] != color)
                continue;

            visited[currentIndex] = true;
            group.Add(currentIndex);

            int row = currentIndex / columns;
            int col = currentIndex % columns;

            TryAddNeighbor(row - 1, col, color, queue, grid, visited, groupIDGrid, rows, columns);
            TryAddNeighbor(row + 1, col, color, queue, grid, visited, groupIDGrid, rows, columns);
            TryAddNeighbor(row, col - 1, color, queue, grid, visited, groupIDGrid, rows, columns);
            TryAddNeighbor(row, col + 1, color, queue, grid, visited, groupIDGrid, rows, columns);
        }

        queue.Dispose();
    }

    private void TryAddNeighbor(int row, int col, int color, NativeQueue<int> queue, NativeArray<int> grid, NativeArray<bool> visited, NativeArray<int> groupIDGrid, int rows, int columns)
    {
        if (row < 0 || row >= rows || col < 0 || col >= columns)
            return;

        int neighborIndex = row * columns + col;

        if (!visited[neighborIndex] && grid[neighborIndex] == color)
        {
            queue.Enqueue(neighborIndex);
        }
    }

    private void UpdateSortingOrder(NativeArray<Entity> gridEntities, int rows, int columns)
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                int index = row * columns + col;

                var entity = gridEntities[index];
                if (entity != Entity.Null && EntityManager.HasComponent<SpriteRenderer>(entity))
                {
                    var spriteRenderer = EntityManager.GetComponentObject<SpriteRenderer>(entity);
                    spriteRenderer.sortingOrder = row; // Hücre satırına göre sorting order güncellenir
                }
            }
        }
    }
}
