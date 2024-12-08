using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using UnityEngine;

public partial class BlastGroupSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Kullanıcı fare tıklamasını kontrol et
        if (!Input.GetMouseButtonDown(0)) return;

        // Mouse pozisyonunu al ve dünya koordinatlarına çevir
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        int2 gridPosition = new int2((int)math.floor(worldPos.x), (int)math.floor(worldPos.y));
        Debug.Log($"Lokasyon: {gridPosition}");
        // Seçilen hücreye ait grubu bulun
        NativeList<Entity> groupEntities = FindGroupEntities(gridPosition);

        // Grubu patlat
        if (groupEntities.Length > 1)
        {
            foreach (var entity in groupEntities)
            {
                Debug.Log($"Patlatılan Entity: {entity}");
                EntityManager.DestroyEntity(entity);
            }
        }

        // Belleği temizle
        groupEntities.Dispose();
    }

    private NativeList<Entity> FindGroupEntities(int2 startPos)
    {
        NativeList<Entity> groupEntities = new NativeList<Entity>(Allocator.TempJob);
        NativeQueue<int2> queue = new NativeQueue<int2>(Allocator.Temp);
        queue.Enqueue(startPos);

        var visited = new NativeHashSet<int2>(10, Allocator.TempJob); // Ziyaret edilen pozisyonlar
        int groupColor = -1;

        while (queue.Count > 0)
        {
            int2 current = queue.Dequeue();

            if (visited.Contains(current))
                continue;

            visited.Add(current);

            // Pozisyondaki Entity'yi bulun
            var entities = EntityManager.GetAllEntities(Allocator.Temp);
            foreach (var entity in entities)
            {
                if (EntityManager.HasComponent<LocalTransform>(entity) &&
                    EntityManager.HasComponent<TileData>(entity))
                {
                    var transform = EntityManager.GetComponentData<LocalTransform>(entity);
                    var tileData = EntityManager.GetComponentData<TileData>(entity);

                    int2 entityPos = new int2((int)math.round(transform.Position.x), (int)math.round(transform.Position.y));

                    if (entityPos.Equals(current))
                    {
                        if (groupColor == -1)
                        {
                            groupColor = tileData.ColorIndex; // Grubun rengini belirle
                        }

                        if (tileData.ColorIndex == groupColor)
                        {
                            groupEntities.Add(entity);

                            // Komşu hücreleri sıraya ekle
                            queue.Enqueue(new int2(current.x + 1, current.y));
                            queue.Enqueue(new int2(current.x - 1, current.y));
                            queue.Enqueue(new int2(current.x, current.y + 1));
                            queue.Enqueue(new int2(current.x, current.y - 1));
                        }
                    }
                }
            }

            entities.Dispose();
        }

        visited.Dispose();
        queue.Dispose();

        return groupEntities;
    }
}
