using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

public partial class GroupDetectionSystem : SystemBase
{
    private SpriteArrayComponent spriteArrayComponent;

    protected override void OnUpdate()
    {
        
    }

    protected override void OnStartRunning()
    {
        // SubScene'deki SpriteArrayComponent'i almak
        Entities
            .WithAll<SpriteArrayComponent>()
            .ForEach((Entity entity, SpriteArrayComponent spriteComponent) =>
            {
                spriteArrayComponent = spriteComponent;
            }).WithoutBurst().Run();

        if (spriteArrayComponent == null)
        {
            Debug.LogError("SpriteArrayComponent bulunamadı! SubScene'den eklenmiş olmalıdır.");
            return;
        }

        var gridQuery = GetEntityQuery(typeof(GridComponent));
        if (gridQuery.CalculateEntityCount() == 0)
        {
            Debug.LogError("GridComponent içeren hiçbir entity bulunamadı!");
            return;
        }

        var gridEntity = gridQuery.GetSingletonEntity();
        var gridComponent = EntityManager.GetComponentData<GridComponent>(gridEntity);

        var visited = new NativeArray<bool>(gridComponent.GridData.Length, Allocator.TempJob);
        var group = new NativeList<int>(Allocator.TempJob);
        var workQueue = new NativeQueue<int>(Allocator.TempJob);

        var grid = gridComponent.GridData;
        int rows = gridComponent.Rows;
        int columns = gridComponent.Columns;

        // Grid üzerinde grup algılama işlemi
        Entities
            .WithAll<BoardInitializationSystem.GridEntityBuffer>()
            .ForEach((DynamicBuffer<BoardInitializationSystem.GridEntityBuffer> gridBuffer) =>
            {
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < columns; col++)
                    {
                        int index = row * columns + col;

                        // Zaten ziyaret edilmiş veya boş hücreleri atla
                        if (grid[index] == 0 || visited[index]) continue;

                        int targetColor = grid[index];

                        // Grup algılama işini bir job olarak çalıştır
                        var job = new GroupDetectionJob
                        {
                            Grid = grid,
                            Rows = rows,
                            Columns = columns,
                            StartIndex = index,
                            TargetColor = targetColor,
                            Visited = visited,
                            Group = group,
                            WorkQueue = workQueue.AsParallelWriter()
                        };

                        job.Run();
                       

                        // Grup tespit edildiyse sprite güncelle
                        group.SortJob().Schedule();
                        Sprite newSprite = GetSpriteForGroupSize(group.Length,targetColor);
                        if (newSprite != null)
                        {
                            foreach (var cellIndex in group)
                            {
                                int rowx = cellIndex / columns; // Hücre satırı
                             

                                var tileEntity = gridBuffer[cellIndex].Entity;
                                // SpriteRenderer bileşenini güncelle
                                if (EntityManager.HasComponent<SpriteRenderer>(tileEntity))
                                {
                                    var spriteRenderer = EntityManager.GetComponentObject<SpriteRenderer>(tileEntity);
                                    spriteRenderer.sortingOrder =  rowx;
                                    spriteRenderer.sprite = newSprite;
                                   
                                    Debug.Log($"Sprite değiştirildi: Entity={tileEntity}, Sprite={newSprite.name}");
                                }
                            }
                       }int entityIndex = gridBuffer[index].Entity.Index;
                        Debug.Log($"Grid Index: {index}, Entity Index: {entityIndex}, GridValue: {grid[index]}");

                        group.Clear(); // Grup listesini sıfırla
                    }
                }
            }).WithoutBurst().Run();

        visited.Dispose();
        group.Dispose();
        workQueue.Dispose();
    }

    private Sprite GetSpriteForGroupSize(int groupSize, int colorIndex)
    {
        // Mapping'den ColorID'ye uygun sprite'ı bul
        foreach (var mapping in spriteArrayComponent.mappings)
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

    [BurstCompile]
    private struct GroupDetectionJob : IJob
    {
        [ReadOnly] public NativeArray<int> Grid;
        public int Rows;
        public int Columns;
        public int StartIndex;
        public int TargetColor;

        public NativeArray<bool> Visited;
        public NativeList<int> Group;
        public NativeQueue<int>.ParallelWriter WorkQueue;

        public void Execute()
        {
            var localQueue = new NativeQueue<int>(Allocator.Temp);
            localQueue.Enqueue(StartIndex);
            Visited[StartIndex] = true;

            while (localQueue.TryDequeue(out int currentIndex))
            {
                Group.Add(currentIndex);

                int currentRow = currentIndex / Columns;
                int currentCol = currentIndex % Columns;

                AddNeighbor(localQueue, currentRow - 1, currentCol); // Yukarı
                AddNeighbor(localQueue, currentRow, currentCol - 1); // Sol
                AddNeighbor(localQueue, currentRow + 1, currentCol); // Aşağı
                AddNeighbor(localQueue, currentRow, currentCol + 1); // Sağ
            }

            localQueue.Dispose();
        }

        private void AddNeighbor(NativeQueue<int> queue, int row, int col)
        {
            if (row >= 0 && row < Rows && col >= 0 && col < Columns)
            {
                int neighborIndex = row * Columns + col;

                if (!Visited[neighborIndex] && Grid[neighborIndex] == TargetColor)
                {
                    Visited[neighborIndex] = true;
                    queue.Enqueue(neighborIndex);
                }
            }
        }
 
    }
}
