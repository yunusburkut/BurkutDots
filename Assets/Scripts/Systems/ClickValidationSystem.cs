using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

[BurstCompile]
public partial class ClickValidationSystem : SystemBase
{
    protected override void OnUpdate() { }

    public void ProcessClick(Vector3 worldPos)
    {
        // BoardState'e erişim
        if (!SystemAPI.TryGetSingleton<BoardState>(out var boardState))
        {
            Debug.LogError("BoardState bulunamadı. BoardInitializationSystem'in çalıştığından emin olun.");
            return;
        }

        var grid = boardState.Grid;
        var groupIDGrid = boardState.GroupIDGrid;
        var gridEntities = boardState.GridEntities;
        int rows = boardState.Rows;
        int columns = boardState.Columns;

        // Dünya pozisyonunu grid pozisyonuna çevir
        int2 gridPosition = new int2((int)math.floor(worldPos.x), (int)math.floor(worldPos.y));

        // Tıklanan pozisyonun geçerliliğini kontrol et
        if (!IsWithinGrid(gridPosition, columns, rows))
        {
            Debug.Log("Tıklanan pozisyon grid dışında.");
            return;
        }

        int index = gridPosition.y * columns + gridPosition.x;

        // Geçersiz hücre kontrolü
        if (grid[index] < 0)
        {
            Debug.Log("Tıklanan hücre boş veya engel içeriyor.");
            return;
        }

        int groupID = groupIDGrid[index]; // Tıklanan hücrenin GroupID'sini al

        if (groupID < 0)
        {
            return;
        }

        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        // GroupID'ye ait tüm hücreleri yok et
        for (int i = 0; i < grid.Length; i++)
        {
            if (groupIDGrid[i] == groupID)
            {
                grid[i] = -1; // Hücreyi boş yap
                groupIDGrid[i] = -1; // GroupID'yi sıfırla
                ecb.DestroyEntity(gridEntities[i]); // Entity'yi yok et
                gridEntities[i] = Entity.Null;
            }
        }

        // EntityCommandBuffer işlemini uygula
        ecb.Playback(EntityManager);
        ecb.Dispose();

      
        // FillEmptyTilesSystem'i çağır
        TriggerFillEmptyTiles();
    }

    private void TriggerFillEmptyTiles()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var fillEmptyTilesSystem = world.GetExistingSystemManaged<FillEmptyTilesSystem>();

        if (fillEmptyTilesSystem != null)
        {
            fillEmptyTilesSystem.RunFill();
        }
        else
        {
            Debug.LogError("FillEmptyTilesSystem bulunamadı!");
        }
    }

    private bool IsWithinGrid(int2 pos, int columns, int rows)
    {
        return pos.x >= 0 && pos.x < columns && pos.y >= 0 && pos.y < rows;
    }
}
