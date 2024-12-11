using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

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
        float xOffset = -(columns / .5f); // Sol tarafa kaydırma
        float yOffset = -(rows / .5f);    // Aşağıya kaydırma
         SpriteArrayAuthoring colorSpriteManager;
        colorSpriteManager = Object.FindObjectOfType<SpriteArrayAuthoring>();
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                var tileEntity = entityManager.Instantiate(tilePrefab);
    
                int colorIndex = UnityEngine.Random.Range(0, colorCount);
        
              
                entityManager.SetComponentData(tileEntity, new TileData
                {
                    ColorIndex = colorIndex,
                });

                entityManager.SetComponentData(tileEntity, new LocalTransform
                {
                    Position = new float3(col, row, 0),
                    Rotation = quaternion.identity,
                    Scale = .45f
                });
                if (entityManager.HasComponent<SpriteRenderer>(tileEntity))
                {
                    var spriteRenderer = entityManager.GetComponentObject<SpriteRenderer>(tileEntity);
                    spriteRenderer.sprite = colorSpriteManager.mappings[colorIndex].Sprites[0];//ilk spriteyi kullan
                    spriteRenderer.sortingOrder = row; // Satır numarasını sortingOrder olarak kullan
                    
                }
                else
                {
                    Debug.LogWarning("TileEntity'de SpriteRenderer bileşeni bulunamadı!");
                }
               
            }
        }
    }
}