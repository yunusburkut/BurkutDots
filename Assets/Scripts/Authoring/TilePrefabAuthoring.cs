using UnityEngine;
using Unity.Entities;

public class TilePrefabAuthoring : MonoBehaviour
{
    public GameObject TilePrefab; //Prefab bağlantısı
}

public class TilePrefabBaker : Baker<TilePrefabAuthoring>
{
    public override void Bake(TilePrefabAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        var prefabEntity = GetEntity(authoring.TilePrefab, TransformUsageFlags.Dynamic);

        AddComponent(entity, new TilePrefabComponent
        {
            PrefabEntity = prefabEntity
        });
    }
}

