using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class GroupClickAndDeleteSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem commandBufferSystem;

    protected override void OnCreate()
    {
        // CommandBuffer sistemini al
        commandBufferSystem = World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        // Grid bileşenini al
        var gridQuery = GetEntityQuery(typeof(GridComponent));
        if (gridQuery.CalculateEntityCount() == 0)
        {
            Debug.LogError("GridComponent içeren hiçbir entity bulunamadı!");
            return;
        }

        var gridEntity = gridQuery.GetSingletonEntity();
        var gridComponent = EntityManager.GetComponentData<GridComponent>(gridEntity);
        var grid = new NativeArray<int>(gridComponent.GridData, Allocator.TempJob);
        int rows = gridComponent.Rows;
        int columns = gridComponent.Columns;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        int2 gridPosition = new int2(
            (int)math.floor(worldPos.x),
            (int)math.floor(worldPos.y)
        );

        if (gridPosition.x < 0 || gridPosition.x >= columns || gridPosition.y < 0 || gridPosition.y >= rows)
        {
            Debug.Log("Tıklanan pozisyon grid dışında.");
            return;
        }

        int clickedIndex = gridPosition.y * columns + gridPosition.x;

        if (grid[clickedIndex] == 0)
        {
            Debug.Log("Tıklanan hücre boş.");
            return;
        }

        int targetColor = grid[clickedIndex];
        var visited = new NativeArray<bool>(grid.Length, Allocator.TempJob);
        var group = new NativeList<int>(Allocator.TempJob);

        var ecb = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        // Grup algılama işlemi
        Entities
            .WithAll<TileData>() // TileData bileşeni olan Entity'leri hedefler
            .ForEach((Entity entity, int entityInQueryIndex, ref TileData tileData, in LocalTransform transform) =>
            {
                int row = (int)math.floor(transform.Position.y);
                int col = (int)math.floor(transform.Position.x);
                int index = row * columns + col;

                // Eğer zaten ziyaret edildiyse veya renk uymuyorsa atla
                if (visited[index] || grid[index] != targetColor)
                    return;

                // Grubu işaretle ve kaydet
                visited[index] = true;
                group.Add(index);
            }).Run(); // Algılama işlemi ana thread'de tamamlanır

        if (group.Length <= 1)
        {
            Debug.Log("Grup algılanamadı veya yalnızca bir hücre tespit edildi.");
            visited.Dispose();
            group.Dispose();
            grid.Dispose();
            return;
        }

        Debug.Log($"Grup Tespit Edildi: Boyut={group.Length}, Renk={targetColor}");

        // Paralel silme işlemi
        Entities
            .WithAll<TileData>()
            .ForEach((Entity entity, int entityInQueryIndex, ref TileData tileData, in LocalTransform transform) =>
            {
                int row = (int)math.floor(transform.Position.y);
                int col = (int)math.floor(transform.Position.x);
                int index = row * columns + col;

                // Eğer grup içinde değilse atla
                if (!group.Contains(index))
                    return;

                // Silme işlemini gerçekleştir
                ecb.DestroyEntity(entityInQueryIndex, entity);
            }).ScheduleParallel();

        commandBufferSystem.AddJobHandleForProducer(Dependency);

        // Bellek temizleme
        visited.Dispose();
        group.Dispose();
        grid.Dispose();
    }
}
