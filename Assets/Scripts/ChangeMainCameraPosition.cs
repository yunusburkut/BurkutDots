using UnityEngine;

public class ChangeMainCameraPosition : MonoBehaviour
{
    public Vector3 newPosition;
    private MapSettings mapSettings;
    void Start()
    {
        mapSettings = Object.FindFirstObjectByType<MapSettings>();
        if (!mapSettings)
        {
            Debug.LogError("MapSettings bileşeni sahnede bulunamadı!");
            return;
        }
        // Ana kamerayı kontrol et
        if (Camera.main != null)
        {
            float m = mapSettings.M;
            float n = mapSettings.N;
            m = m / 2;
            n = n / 2;
            newPosition = new Vector3(m, n, -10);
            // Ana kameranın pozisyonunu row sayısına göre değiştiriyoruz uzaklıgınıda ona göre ayarlıyoruz
            Camera.main.transform.position = newPosition;
            Camera.main.orthographicSize = n;
            Debug.Log("Ana kameranın pozisyonu değiştirildi: " + newPosition);
        }
        else
        {
            Debug.LogError("Ana kamera bulunamadı!");
        }
    }
}