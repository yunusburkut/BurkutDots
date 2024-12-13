using Unity.Entities;
using UnityEngine;

public class ClickDetectionSystem : MonoBehaviour
{
    private Camera mainCamera;
    private ClickValidationSystem clickValidationSystem;

    void Start()
    {
        mainCamera = Camera.main;
        var world = World.DefaultGameObjectInjectionWorld;
        clickValidationSystem = world.GetOrCreateSystemManaged<ClickValidationSystem>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) //Sol fare tıklaması
        {
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            clickValidationSystem.ProcessClick(worldPos);
        }
    }
}