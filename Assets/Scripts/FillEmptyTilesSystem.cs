using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class FillEmptyTilesSystem : SystemBase
{
    private BlastGroupSystem blastGroupSystem;

    protected override void OnCreate()
    {
        // BlastGroupSystem'e erişim
        blastGroupSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<BlastGroupSystem>();
    }

    protected override void OnUpdate()
    {
        var grid = blastGroupSystem.GetGrid();
        int rows = 10;
        int columns = 10;

        // Sütun bazlı işlem yap
        for (int col = 0; col < columns; col++)
        {
            int writeRow = 0; // Yeni pozisyon için yazma işaretçisi

            // Grid'i alt satırdan üst satıra tarayın
            for (int row = 0; row < rows; row++)
            {
                int index = row * columns + col;

                if (grid[index] != -1) // Dolu hücre
                {
                    if (row != writeRow) // Kaydırılması gerekiyorsa
                    {
                        int writeIndex = writeRow * columns + col;

                        // Grid güncelle
                        grid[writeIndex] = grid[index];
                        grid[index] = -1;

                        // Entity pozisyonunu güncelle
                        UpdateEntityPosition(col, row, writeRow);
                    }
                    writeRow++; // Yazma işaretçisini bir satır aşağı kaydır
                }
            }

            // Yeni bloklar oluştur ve üst satırdan başlat
            for (int row = writeRow; row < rows; row++)
            {
                int index = row * columns + col;

                if (grid[index] == -1) // Boş hücre
                {
                    int newColor = UnityEngine.Random.Range(0, 6); // Rastgele renk
                    grid[index] = newColor;

                    // Yeni spawner tile'ı ekle
                    AddSpawnerTile(col, row, newColor);
                }
            }
        }
    }

    private void UpdateEntityPosition(int col, int oldRow, int newRow)
    {
        // EntityCommandBuffer oluştur
        EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        Entities
            .WithAll<TileData, LocalTransform>()
            .ForEach((Entity entity, ref LocalTransform transform) =>
            {
                int2 entityPos = new int2((int)math.round(transform.Position.x), (int)math.round(transform.Position.y));

                if (entityPos.x == col && entityPos.y == oldRow)
                {
                    // Hareket bileşeni ekle
                    ecb.AddComponent(entity, new MovingTileComponent
                    {
                        StartPosition = transform.Position,
                        EndPosition = new float3(col, newRow, transform.Position.z),
                        Duration = .25f,
                        ElapsedTime = 0
                    });
                }
            }).WithoutBurst().Run();

        // EntityCommandBuffer'daki değişiklikleri uygula
        Dependency.Complete();
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }

    private void AddSpawnerTile(int col, int row, int colorIndex)
    {
        // Spawner tile'ı yukarıda başlat
        float startRow = 15;

        Entity tilePrefab;
        if (!SystemAPI.TryGetSingleton<TilePrefabComponent>(out var tilePrefabComponent))
        {
            UnityEngine.Debug.LogError("TilePrefabComponent bulunamadı!");
            return;
        }

        tilePrefab = tilePrefabComponent.PrefabEntity;

        Entity newTile = EntityManager.Instantiate(tilePrefab);

        // Başlangıç pozisyonu yukarıda
        float3 startPosition = new float3(col, startRow, 0);
        float3 endPosition = new float3(col, row, 0);

        // Tile'ın başlangıç pozisyonunu ayarla
        EntityManager.SetComponentData(newTile, new LocalTransform
        {
            Position = startPosition,
            Rotation = quaternion.identity,
            Scale = .45f
        });

        EntityManager.SetComponentData(newTile, new TileData
        {
            ColorIndex = colorIndex,
        });

        // Düşüş animasyonu için hareket bileşeni ekle
        EntityManager.AddComponentData(newTile, new MovingTileComponent
        {
            StartPosition = startPosition,
            EndPosition = endPosition,
            Duration = .25f, // Düşme süresi
            ElapsedTime = 0
        });

        // SpriteRenderer ayarları
       
    }
}
