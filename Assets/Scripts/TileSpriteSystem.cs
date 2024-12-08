using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class TileSpriteSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // SpriteArrayComponent'e eriş
        var spriteEntity = GetSingletonEntity<SpriteArrayComponent>();
        var spriteArray = EntityManager.GetComponentObject<SpriteArrayComponent>(spriteEntity);

        if (spriteArray == null || spriteArray.Sprites.Length == 0)
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
                if (tileData.ColorIndex >= 0 && tileData.ColorIndex < spriteArray.Sprites.Length)
                {
                    spriteRenderer.sprite = spriteArray.Sprites[tileData.ColorIndex];
                }
                else
                {
                    Debug.LogError($"Geçersiz ColorIndex: {tileData.ColorIndex}");
                }
            }).WithoutBurst().Run();
    }
}