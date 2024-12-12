using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class TileSpriteSystem : SystemBase
{ private bool hasRunOnce = false;

    protected override void OnUpdate()
    {
        if (hasRunOnce)
        {
            // SpriteArrayComponent'e eriş
            Entity spriteEntity = EntityManager.CreateEntity(typeof(SpriteArrayComponent));
            var spriteArray = EntityManager.GetComponentObject<SpriteArrayComponent>(spriteEntity);

            if (spriteArray == null || spriteArray.mappings[0].Sprites.Length == 0)
            {
                Debug.LogError("SpriteArrayComponent içinde sprite yok!");
                return;
            }

            // Her TileEntity için SpriteRenderer'ı güncelle
            Entities
                .WithAll<TileData>()
                .ForEach((SpriteRenderer spriteRenderer, in TileData tileData) =>
                {
                    // ColorIndex'in geçerli olup olmadığını kontrol et
                    if (tileData.ColorIndex >= 0 && tileData.ColorIndex < spriteArray.mappings.Count)
                    {
                        spriteRenderer.sprite = spriteArray.mappings[tileData.ColorIndex].Sprites[0];
                    }
                    else
                    {
                        Debug.LogError($"Geçersiz ColorIndex: {tileData.ColorIndex}");
                    }
                }).WithoutBurst().Run();
            hasRunOnce = true;
        }
        
    }
}