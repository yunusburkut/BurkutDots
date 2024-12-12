using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public struct GroupDetectionJob : IJob
{
    [ReadOnly] public NativeArray<int> Grid;
    public int Rows;
    public int Columns;
    public int StartIndex;
    public int TargetColor;

    public NativeArray<bool> Visited;
    public NativeList<int> Group;

    public void Execute()
    {
        var localQueue = new NativeQueue<int>(Allocator.Temp);
        localQueue.Enqueue(StartIndex);
        Visited[StartIndex] = true;

        while (localQueue.TryDequeue(out int currentIndex))
        {
            Group.Add(currentIndex);

            int currentRow = currentIndex / Columns;
            int currentCol = currentIndex % Columns;

            AddNeighbor(localQueue, currentRow - 1, currentCol); // Yukarı
            AddNeighbor(localQueue, currentRow + 1, currentCol); // Aşağı
            AddNeighbor(localQueue, currentRow, currentCol - 1); // Sol
            AddNeighbor(localQueue, currentRow, currentCol + 1); // Sağ
        }

        localQueue.Dispose();
    }

    private void AddNeighbor(NativeQueue<int> queue, int row, int col)
    {
        if (row >= 0 && row < Rows && col >= 0 && col < Columns)
        {
            int neighborIndex = row * Columns + col;

            if (!Visited[neighborIndex] && Grid[neighborIndex] == TargetColor)
            {
                Visited[neighborIndex] = true;
                queue.Enqueue(neighborIndex);
            }
        }
    }
}