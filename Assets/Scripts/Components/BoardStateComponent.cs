using Unity.Entities;
using Unity.Collections;

public struct BoardState : IComponentData
{
    public int Rows; // Gridin satır sayısı
    public int Columns; // Gridin sütun sayısı
    public NativeArray<int> Grid; // Hücre durumlarını tutar (-1: boş, -2: engel, >= 0: taş rengi)
    public NativeArray<Entity> GridEntities; // Her hücredeki Entity'leri tutar
}