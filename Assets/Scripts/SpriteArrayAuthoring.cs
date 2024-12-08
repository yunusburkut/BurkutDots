using UnityEngine;
using Unity.Entities;

public class SpriteArrayAuthoring : MonoBehaviour
{
    public Sprite[] Sprites; // Inspector üzerinden atanacak sprite dizisi
}

public class SpriteArrayBaker : Baker<SpriteArrayAuthoring>
{
    public override void Bake(SpriteArrayAuthoring authoring)
    {
        // SubScene'deki GameObject'i Entity'ye dönüştür
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        // SpriteArrayComponent'i global bileşen olarak ekle
        AddComponentObject(entity, new SpriteArrayComponent
        {
            Sprites = authoring.Sprites
        });
    }
}

public class SpriteArrayComponent : IComponentData
{
    public Sprite[] Sprites; // Sprite dizisi
}