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

                    // Yeni bir tile oluştur
                    CreateNewTile(row, col, newColor);
                }
            }
        }
    }

    private void UpdateEntityPosition(int col, int oldRow, int newRow)
    {
        // Eski pozisyondaki entity'yi bul ve yeni pozisyonuna taşı
        Entities
            .WithAll<TileData, LocalTransform>()
            .ForEach((Entity entity, ref LocalTransform transform) =>
            {
                int2 entityPos = new int2((int)math.round(transform.Position.x), (int)math.round(transform.Position.y));

                if (entityPos.x == col && entityPos.y == oldRow)
                {
                    transform.Position = new float3(col, newRow, transform.Position.z); // Yeni pozisyona taşı
                }
            }).WithoutBurst().Run();
    }

    private void CreateNewTile(int row, int col, int colorIndex)
    {
        Entity tilePrefab;
        if (!SystemAPI.TryGetSingleton<TilePrefabComponent>(out var tilePrefabComponent))
        {
            UnityEngine.Debug.LogError("TilePrefabComponent bulunamadı!");
            return;
        }
       
        tilePrefab = tilePrefabComponent.PrefabEntity;

        Entity newTile = EntityManager.Instantiate(tilePrefab);

        EntityManager.SetComponentData(newTile, new TileData
        {
            ColorIndex = colorIndex,
        });

        EntityManager.SetComponentData(newTile, new LocalTransform
        {
            Position = new float3(col, row, 0),//dotween ile düşürmee animasyonunu burda yapacaksın yukarıda başlatıp gelen row+5'den row'a gidecek şekilde row+5 olmasının sebebi ekrarnın dısından dusme eefekti vermek
            Rotation = quaternion.identity,
            Scale = .45f
        });
        if (EntityManager.HasComponent<SpriteRenderer>(newTile))
        {
            var spriteRenderer = EntityManager.GetComponentObject<SpriteRenderer>(newTile);
            // spriteRenderer.sprite = colorSpriteManager.mappings[colorIndex].Sprites[0];//ilk spriteyi kullan
            spriteRenderer.sortingOrder = row; // Satır numarasını sortingOrder olarak kullan
            Debug.Log($"TileEntity için Sorting Order Ayarlandı: {row}");
        }
    }
}
