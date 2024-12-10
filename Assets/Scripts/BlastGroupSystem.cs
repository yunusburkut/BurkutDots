using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using UnityEngine;

public partial class BlastGroupSystem : SystemBase
{
    private NativeArray<int> grid; // Grid durumu
    private int rows = 10;
    private int columns = 10;

    protected override void OnCreate()
    {
        grid = new NativeArray<int>(rows * columns, Allocator.Persistent);

        for (int i = 0; i < grid.Length; i++)
        {
            grid[i] = 0; // Başlangıçta dolu hücreler
        }
    }

    protected override void OnDestroy()
    {
        if (grid.IsCreated)
        {
            grid.Dispose();
        }
    }

    protected override void OnUpdate()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        int2 gridPosition = new int2((int)math.floor(worldPos.x), (int)math.floor(worldPos.y));

        NativeList<Entity> groupEntities = FindGroupEntities(gridPosition);

        if (groupEntities.Length > 1)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            foreach (var entity in groupEntities)
            {
                // Entity'nin pozisyonunu bul ve grid'i güncelle
                if (EntityManager.HasComponent<LocalTransform>(entity))
                {
                    var transform = EntityManager.GetComponentData<LocalTransform>(entity);
                    int row = (int)math.round(transform.Position.y);
                    int col = (int)math.round(transform.Position.x);
                    int index = row * columns + col;

                    if (index >= 0 && index < grid.Length)
                    {
                        grid[index] = -1; // Patlayan hücre
                    }
                }

                ecb.DestroyEntity(entity);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        groupEntities.Dispose();

        // Dependecy diğer sistem için senkronize edilir
        Dependency.Complete();
    }

    private NativeList<Entity> FindGroupEntities(int2 startPos)
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

            Entities
                .WithAll<LocalTransform, TileData>()
                .ForEach((Entity entity, in LocalTransform transform, in TileData tileData) =>
                {
                    int2 entityPos = new int2((int)math.round(transform.Position.x), (int)math.round(transform.Position.y));

                    if (entityPos.Equals(current))
                    {
                        if (groupColor == -1)
                        {
                            groupColor = tileData.ColorIndex;
                        }

                        if (tileData.ColorIndex == groupColor)
                        {
                            groupEntities.Add(entity);

                            queue.Enqueue(new int2(current.x + 1, current.y));
                            queue.Enqueue(new int2(current.x - 1, current.y));
                            queue.Enqueue(new int2(current.x, current.y + 1));
                            queue.Enqueue(new int2(current.x, current.y - 1));
                        }
                    }
                }).Run();
        }

        visited.Dispose();
        queue.Dispose();

        return groupEntities;
    }

    public NativeArray<int> GetGrid()
    {
        return grid; // Diğer sistemin kullanması için grid'i paylaşır
    }
}
