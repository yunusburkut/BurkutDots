using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using UnityEngine;

public partial class GroupDetectionSystem : SystemBase
{
    protected override void OnUpdate()
    {
        
        int rows = 10;       // Tahta boyutları
        int columns = 10;    // Tahta boyutları

        // Entity'lerin listesi
        var entities = EntityManager.GetAllEntities(Unity.Collections.Allocator.Temp);
        var grid = new NativeArray<int>(rows * columns, Allocator.TempJob); // Renk bilgisi
        var visited = new NativeArray<bool>(rows * columns, Allocator.TempJob); // Ziyaret bilgisi

        // Entity'lerden renk bilgisi al
        for (int i = 0; i < entities.Length; i++)
        {
            if (EntityManager.HasComponent<TileData>(entities[i]))
            {
                var tileData = EntityManager.GetComponentData<TileData>(entities[i]);

                // Pozisyon hesapla (satır/sütun pozisyonuna göre)
                var transform = EntityManager.GetComponentData<LocalTransform>(entities[i]);
                int row = (int)math.round(transform.Position.y);
                int col = (int)math.round(transform.Position.x);

                int index = row * columns + col;

                if (index >= 0 && index < grid.Length)
                {
                    grid[index] = tileData.ColorIndex; // Renk bilgisi
                }
            }
        }

        // Grupları tespit etmek için Flood Fill
        NativeList<int2> groupPositions = new NativeList<int2>(Allocator.TempJob);
        NativeList<int> groupColors = new NativeList<int>(Allocator.TempJob);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                int index = i * columns + j;

                // Ziyaret edilmemiş ve geçerli bir renkse
                if (!visited[index] && grid[index] >= 0)
                {
                    // Yeni bir grup oluştur
                    NativeList<int2> currentGroup = new NativeList<int2>(Allocator.Temp);
                    FloodFill(i, j, rows, columns, grid, visited, grid[index], currentGroup);

                    // Eğer grup 2 veya daha fazla elemandan oluşuyorsa
                    if (currentGroup.Length >= 2)
                    {
                        groupPositions.AddRange(currentGroup.AsArray());
                        groupColors.Add(grid[index]);
                    }
                    currentGroup.Dispose();
                }
            }
        }

        // Belleği temizle
        entities.Dispose();
        grid.Dispose();
        visited.Dispose();
        groupPositions.Dispose();
        groupColors.Dispose();
    }
    
    private void FloodFill(
        int startRow, int startCol,
        int rows, int columns,
        NativeArray<int> grid,
        NativeArray<bool> visited,
        int color,
        NativeList<int2> group)
    {
        NativeQueue<int2> queue = new NativeQueue<int2>(Allocator.Temp);
        queue.Enqueue(new int2(startRow, startCol));
        
        while (queue.Count > 0)
        {
            int2 current = queue.Dequeue();
            int index = current.x * columns + current.y;
        
            if (current.x < 0 || current.x >= rows || current.y < 0 || current.y >= columns)
                continue; // Sınırların dışında
        
            if (visited[index])
                continue; // Zaten ziyaret edilmiş
        
            if (grid[index] != color)
                continue; // Aynı renk değil
        
            // Hücreyi işaretle
            visited[index] = true;
            group.Add(current);
        
            // Komşuları sıraya ekle
            queue.Enqueue(new int2(current.x + 1, current.y)); // Aşağı
            queue.Enqueue(new int2(current.x - 1, current.y)); // Yukarı
            queue.Enqueue(new int2(current.x, current.y + 1)); // Sağ
            queue.Enqueue(new int2(current.x, current.y - 1)); // Sol
        }
        
        queue.Dispose();
    }
}
