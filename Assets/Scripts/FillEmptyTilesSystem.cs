using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;

public partial class FillEmptyTilesSystem : SystemBase
{
    private BlastGroupSystem blastGroupSystem;

    protected override void OnCreate()
    {
        // BlastGroupSystem'e erişim
        blastGroupSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<BlastGroupSystem>();
    }

    protected override void OnUpdate()
    {
        var grid = blastGroupSystem.GetGrid();
        int rows = 10;
        int columns = 10;

        // Her sütun için boş hücreleri aşağıya kaydır
        for (int col = 0; col < columns; col++)
        {
            int emptyRow = -1;

            for (int row = 0; row < rows; row++)
            {
                int index = row * columns + col;

                if (grid[index] == -1) // Boş hücre bulundu
                {
                    if (emptyRow == -1)
                        emptyRow = row; // İlk boş hücreyi kaydet
                }
                else if (emptyRow != -1) // Üstte dolu hücre varsa kaydır
                {
                    int filledIndex = emptyRow * columns + col;

                    // Grid güncelleme
                    grid[filledIndex] = grid[index];
                    grid[index] = -1;

                    // Entity'yi güncelle
                    Entities
                        .WithAll<TileData, LocalTransform>()
                        .ForEach((Entity entity, ref LocalTransform transform) =>
                        {
                            int2 entityPos = new int2((int)math.round(transform.Position.x), (int)math.round(transform.Position.y));

                            if (entityPos.y == row && entityPos.x == col)
                            {
                                transform.Position = new float3(col, emptyRow, transform.Position.z); // Pozisyonu güncelle
                            }
                        }).Run();

                    emptyRow++; // Sonraki boş hücreye geç
                }
            }
        }
    }
}
