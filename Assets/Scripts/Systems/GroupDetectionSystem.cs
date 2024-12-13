using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using UnityEngine;
[BurstCompile]
public partial class GroupDetectionSystem : SystemBase
{
    protected override void OnUpdate()
    {
        
    }
    private MapSettings mapSettings;
    private SpriteArrayAuthoring colorSpriteManager;
    private int rows;
    private int columns;
    private int BlastableArrayCount = 0;
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
        Enabled = false;
        RunDetection();
    }
    
    public void RunDetection()
    {
        // BoardState'e erişim
        if (!SystemAPI.TryGetSingleton<BoardState>(out var boardState))
        {
            Debug.LogError("BoardState bulunamadı. BoardInitializationSystem'in çalıştığından emin olun.");
            return;
        }
        BlastableArrayCount = 0;
        var grid = boardState.Grid;
        rows = boardState.Rows;
        columns = boardState.Columns;

        // Ziyaret edilen hücreleri işaretlemek için geçici bir NativeArray oluşturuyoruz
        NativeArray<bool> visited = new NativeArray<bool>(grid.Length, Allocator.Temp);
     
   
        for (int index = 0; index < grid.Length; index++)
        {
            if (visited[index] || grid[index] < 0 || grid[index] == -2)
                continue; 

            var group = new NativeList<int>(Allocator.Temp);
            FindGroup(index, grid[index], group, grid, visited);
            Sprite selectedSprite = GetSpriteForGroupSize(group.Length, grid[index]);

            if (group.Length > 1) //Minimum grup boyutu kontrolü, dynamic yapılabiilirdi 
            {
                if (selectedSprite != null)
                {
                    UpdateGroupSprites(group, selectedSprite, grid);
                }
                BlastableArrayCount++;
            }
            else if (group.Length <= 1)
            {
                if (selectedSprite != null)
                {
                    UpdateGroupSprites(group, selectedSprite, grid);
                }
            }
            group.Dispose();
        }
        if (BlastableArrayCount <=0)
        {
            DeadlockShuffle();
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
            if (grid[currentIndex] == -2 || grid[currentIndex] == -1)
                continue;
            
            visited[currentIndex] = true;
            group.Add(currentIndex);
            int row = currentIndex / columns;
            int col = currentIndex % columns;
            
            //Komşuları tara
            TryAddNeighbor(row - 1, col, color, queue, grid, visited); 
            TryAddNeighbor(row + 1, col, color, queue, grid, visited);
            TryAddNeighbor(row, col - 1, color, queue, grid, visited); 
            TryAddNeighbor(row, col + 1, color, queue, grid, visited);
        }
        queue.Dispose();
    }

    private void TryAddNeighbor(int row, int col, int color, NativeQueue<int> queue, NativeArray<int> grid, NativeArray<bool> visited)
    {
        //Grid sınırlarının dışında ise ekleme
        if (row < 0 || row >= rows || col < 0 || col >= columns)
            return;

        int neighborIndex = row * columns + col;
        
        if (!visited[neighborIndex] && grid[neighborIndex] == color && grid[neighborIndex] != -2 )
        {
            queue.Enqueue(neighborIndex);
        }
    }

    private Sprite GetSpriteForGroupSize(int groupSize, int colorIndex)
    {//inspector'dan atadıgımız spriteları çekiyor
        foreach (var mapping in colorSpriteManager.mappings)
        {
            if (mapping.ColorID == colorIndex)
            {
                if (groupSize < mapSettings.A && mapping.Sprites.Length > 0)
                {
                    return mapping.Sprites[0];
                }
                else if (groupSize >= mapSettings.A && mapping.Sprites.Length > 1)
                {
                    return mapping.Sprites[1];
                }
                else if (groupSize >= mapSettings.B && mapping.Sprites.Length > 2)
                {
                    return mapping.Sprites[2];
                }
                else if (groupSize >= mapSettings.C && mapping.Sprites.Length > 3)
                {
                    return mapping.Sprites[3];
                }
            }
        }

        Debug.LogWarning($"Grup boyutu {groupSize} ve renk {colorIndex} için uygun sprite bulunamadı.");
        return null;
    }

    private void UpdateGroupSprites(NativeList<int> group, Sprite selectedSprite, NativeArray<int> grid)
    {
        foreach (int cellIndex in group)
        {
            int row = cellIndex / columns;
            int col = cellIndex % columns;

            if (grid[cellIndex] == -2) 
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

    private void DeadlockShuffle()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        WeightedShuffleBoardSystem ShuffleBoardSystemTri = world.GetOrCreateSystemManaged<WeightedShuffleBoardSystem>();
        ShuffleBoardSystemTri.ShuffleBoard();
    }
}
