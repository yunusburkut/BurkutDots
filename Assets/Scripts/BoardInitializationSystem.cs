using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial class BoardInitializationSystem : SystemBase
{
    private Entity tilePrefab;
    protected override void OnUpdate()
    {
        // Sistem sürekli güncellemeye ihtiyaç duymuyorsa burası boş bırakılır
    }
    protected override void OnStartRunning()
    {
        if (!SystemAPI.TryGetSingleton<TilePrefabComponent>(out var tilePrefabComponent))
        {
            UnityEngine.Debug.LogError("TilePrefabComponent bulunamadı!");
            return;
        }
       
        tilePrefab = tilePrefabComponent.PrefabEntity;
        
        CreateBoard();
    }

    private void CreateBoard()
    {
        int rows = 10, columns = 10, colorCount = 6;
        var entityManager = EntityManager;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                var tileEntity = entityManager.Instantiate(tilePrefab);

                int colorIndex = UnityEngine.Random.Range(0, colorCount);

              
                entityManager.SetComponentData(tileEntity, new TileData
                {
                    ColorIndex = colorIndex
                });

                entityManager.SetComponentData(tileEntity, new LocalTransform
                {
                    Position = new float3(col, row, 0),
                    Rotation = quaternion.identity,
                    Scale = 1f/2.24f
                });
            }
        }
    }
}