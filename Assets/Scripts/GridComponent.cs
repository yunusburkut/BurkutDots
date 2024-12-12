using Unity.Collections;
using Unity.Entities;

public struct GridComponent : IComponentData
{
    public NativeArray<int> GridData;
    public int Rows;
    public int Columns;
}