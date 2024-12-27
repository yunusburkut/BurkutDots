using Unity.Collections;
using Unity.Entities;

public struct BoardState : IComponentData
{
    public int Rows;
    public int Columns;
    public NativeArray<int> Grid; // Hücre durumları
    public NativeArray<int> GroupIDGrid; // GroupID'leri tutar
    public NativeArray<Entity> GridEntities;
}