using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

[System.Serializable]
public class ColorSpriteArray
{
    public int ColorID;         // Renk ID
    public Sprite[] Sprites;    // Bu renk için kullanılacak sprite dizisi
}
public class SpriteArrayAuthoring : MonoBehaviour
{
    public List<ColorSpriteArray> mappings; 
}

public class SpriteArrayBaker : Baker<SpriteArrayAuthoring>
{
    public override void Bake(SpriteArrayAuthoring authoring)
    {
        //SubScene'deki GameObject'i Entity'ye dönüştürüyoruz
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        //SpriteArrayComponent'i ekliiyoruz
        AddComponentObject(entity, new SpriteArrayComponent
        {
            mappings = authoring.mappings
        });
    }
}

public class SpriteArrayComponent : IComponentData
{
    public List<ColorSpriteArray> mappings; 
}