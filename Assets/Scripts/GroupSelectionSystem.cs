using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class GroupSelectionSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // // Kullanıcı fare tıklamasını kontrol et
        // if (!Input.GetMouseButtonDown(0)) return;
        //
        // // Mouse pozisyonunu al ve dünya koordinatlarına çevir
        // Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        // int2 gridPosition = new int2((int)math.floor(worldPos.x), (int)math.floor(worldPos.y));
        //
        // Debug.Log($"Seçilen Pozisyon: {gridPosition}");
        //
        // // Burada seçilen pozisyondan grubun tespit edilmesi gerekiyor
        // // GroupDetectionSystem ile bulunan grubu kontrol edebilirsiniz.
    }
}