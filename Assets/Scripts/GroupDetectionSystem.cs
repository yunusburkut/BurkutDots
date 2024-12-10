using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using UnityEngine;

public partial class GroupDetectionSystem : SystemBase
{
    private SpriteArrayAuthoring colorSpriteManager;
    private bool hasRun = false; // Sistem sadece bir kez çalışacak

    protected override void OnStartRunning()
    {
        // SpriteArrayAuthoring'i sahneden bulun
        colorSpriteManager = Object.FindObjectOfType<SpriteArrayAuthoring>();

        if (colorSpriteManager == null || colorSpriteManager.mappings.Count == 0)
        {
            Debug.LogError("SpriteArrayAuthoring bulunamadı veya mappings boş!");
        }
    }

    protected override void OnUpdate()
    {
        if (hasRun) return; // Sistem sadece bir kez çalışır
        hasRun = true;

        if (colorSpriteManager == null || colorSpriteManager.mappings.Count == 0)
        {
            Debug.LogError("ColorSpriteManager eksik!");
            return;
        }

        int rows = 10;       // Tahta boyutları
        int columns = 10;    // Tahta boyutları
        int totalCells = rows * columns;

        // Entity'lerin listesi
        var entities = EntityManager.GetAllEntities(Allocator.Temp);
        var grid = new NativeArray<int>(totalCells, Allocator.TempJob); // Renk bilgisi
        var visited = new NativeArray<bool>(totalCells, Allocator.TempJob); // Ziyaret bilgisi

        // Grid'i doldur
        foreach (var entity in entities)
        {
            if (EntityManager.HasComponent<TileData>(entity) && EntityManager.HasComponent<LocalTransform>(entity))
            {
                var tileData = EntityManager.GetComponentData<TileData>(entity);
                var transform = EntityManager.GetComponentData<LocalTransform>(entity);

                int row = (int)math.round(transform.Position.y);
                int col = (int)math.round(transform.Position.x);
                int index = row * columns + col;

                if (index >= 0 && index < grid.Length)
                {
                    grid[index] = tileData.ColorIndex; // Renk bilgisi
                }
            }
        }

        // Tek for döngüsü ile grup tespiti ve işleme
        for (int index = 0; index < totalCells; index++)
        {
            if (visited[index] || grid[index] < 0)
                continue; // Zaten ziyaret edilmiş veya geçerli bir renk değil

            var group = new NativeList<int>(Allocator.Temp); // Grup üyelerini saklar
            int currentColor = grid[index];

            // Flood Fill benzeri işlem
            FindGroup(index, rows, columns, grid, visited, currentColor, group);

            // Grup 2'den büyükse işleme devam edin
            
            // Grup için uygun sprite seçimi
            Sprite selectedSprite = GetSpriteForGroupSize(group.Length, currentColor);

            // Grup içindeki tüm tile'ların sprite'ını değiştir
            foreach (int cellIndex in group)
            {
                float3 position = new float3(cellIndex % columns, cellIndex / columns, 0);

                // Entity'yi bul ve sprite'ı değiştir
                foreach (var entity in entities)
                {
                    if (!EntityManager.HasComponent<LocalTransform>(entity) || !EntityManager.HasComponent<SpriteRenderer>(entity))
                        continue;

                    var transform = EntityManager.GetComponentData<LocalTransform>(entity);
                    if (math.all(transform.Position == position))
                    {
                        var spriteRenderer = EntityManager.GetComponentObject<SpriteRenderer>(entity);
                        spriteRenderer.sprite = selectedSprite; // Sprite değişimi
                    }
                }
            
            }
            

            group.Dispose();
        }

        // Belleği temizle
        entities.Dispose();
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

        while (queue.Count > 0)
        {
            int currentIndex = queue.Dequeue();

            if (visited[currentIndex])
                continue;

            visited[currentIndex] = true;
            group.Add(currentIndex);

            int row = currentIndex / columns;
            int col = currentIndex % columns;

            // Komşuları sıraya ekle
            TryAddNeighbor(row - 1, col, rows, columns, currentIndex, grid, visited, color, queue);
            TryAddNeighbor(row + 1, col, rows, columns, currentIndex, grid, visited, color, queue);
            TryAddNeighbor(row, col - 1, rows, columns, currentIndex, grid, visited, color, queue);
            TryAddNeighbor(row, col + 1, rows, columns, currentIndex, grid, visited, color, queue);
        }

        queue.Dispose();
    }

    private void TryAddNeighbor(
        int row,
        int col,
        int rows,
        int columns,
        int currentIndex,
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