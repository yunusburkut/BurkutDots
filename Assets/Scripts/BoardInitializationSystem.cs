using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial class BoardInitializationSystem : SystemBase
{
    private Entity tilePrefab;
    private NativeArray<int> grid; // Grid verisi
    private int rows = 10; // Satır sayısı
    private int columns = 10; // Sütun sayısı

    protected override void OnCreate()
    {
        // Grid verisi oluşturuluyor
        grid = new NativeArray<int>(rows * columns, Allocator.Persistent);
        for (int i = 0; i < grid.Length; i++)
        {
            grid[i] = 0; // Varsayılan başlangıç değeri
        }
    }

    public struct GridEntityBuffer : IBufferElementData
    {
        public Entity Entity; // Her hücre için bir Entity saklanır
    }

    protected override void OnStartRunning()
    {
        if (!SystemAPI.TryGetSingleton<TilePrefabComponent>(out var tilePrefabComponent))
        {
            UnityEngine.Debug.LogError("TilePrefabComponent bulunamadı!");
            return;
        }

        tilePrefab = tilePrefabComponent.PrefabEntity;

        // Grid bileşeni için bir Entity oluştur
        var gridEntity = EntityManager.CreateEntity();
        EntityManager.AddComponentData(gridEntity, new GridComponent
        {
            GridData = grid,
            Rows = rows,
            Columns = columns
        });

        // DynamicBuffer ekle
        var gridBuffer = EntityManager.AddBuffer<GridEntityBuffer>(gridEntity);

        // Hücre Entity'lerini oluştur ve DynamicBuffer'a ekle
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                var tileEntity = EntityManager.Instantiate(tilePrefab);

                // Hücreyi temsil eden Entity'yi buffer'a ekle
                gridBuffer.Add(new GridEntityBuffer { Entity = tileEntity });
            }
        }

        EntityManager.SetName(gridEntity, "GridEntity");

        // Board oluşturuluyor
        CreateBoard(gridBuffer);
    }

    private void CreateBoard(DynamicBuffer<GridEntityBuffer> gridBuffer)
    {
        int colorCount = 6; // Farklı renk sayısı

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                int index = row * columns + col;
                var tileEntity = gridBuffer[index].Entity;

                // Rastgele bir renk seç
                int colorIndex = UnityEngine.Random.Range(1, colorCount);
                grid[index] = colorIndex;

                // TileData bileşeni ekle
                EntityManager.SetComponentData(tileEntity, new TileData
                {
                    ColorIndex = colorIndex,
                });

                // LocalTransform bileşenini ayarla
                EntityManager.SetComponentData(tileEntity, new LocalTransform
                {
                    Position = new float3(col, row, 0),
                    Rotation = quaternion.identity,
                    Scale = 0.45f
                });
            }
        }
    }

    protected override void OnDestroy()
    {
        // Grid belleği serbest bırakılıyor
        if (grid.IsCreated)
        {
            grid.Dispose();
        }
    }

    protected override void OnUpdate()
    {
        // BoardInitialization bir başlangıç sistemi olduğu için OnUpdate genellikle boş bırakılır
    }
}
