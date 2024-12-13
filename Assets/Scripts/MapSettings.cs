using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class MapSettings : MonoBehaviour
{
    [Header("Board Configuration")]
    [Tooltip("Number of rows in the board.")]
    public int M = 10;

    [Tooltip("Number of columns in the board.")]
    public int N = 10;

    [Tooltip("Number of unique colors in the board.")]
    public int K = 6;
    
    [Header("Icon Configuration")]
    [Tooltip("A for first icon")]
    public int A;
    [Tooltip("B for first icon")]
    public int B;
    [Tooltip("C for first icon")]
    public int C;
  
    
    [Header("Obstacle Configuration")]
    [Tooltip("Grid positions of obstacles.")]
    public List<int2> ObstaclePositions;

}