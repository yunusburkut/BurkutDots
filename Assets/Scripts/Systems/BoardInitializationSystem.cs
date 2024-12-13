using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;

[BurstCompile]
public partial class BoardInitializationSystem : SystemBase
{
    private Entity tilePrefab;
    private MapSettings mapSettings;
    protected override void OnUpdate()
    {
    }

    protected override void OnStartRunning()
    {
        mapSettings = Object.FindFirstObjectByType<MapSettings>();
        if (!mapSettings)
        {
            Debug.LogError("MapSettings bileşeni sahnede bulunamadı!");
            Enabled = false;
            return;
        }
        if (!SystemAPI.TryGetSingleton<TilePrefabComponent>(out var tilePrefabComponent))
        {
            UnityEngine.Debug.LogError("TilePrefabComponent bulunamadı!");
            return;
        }

        tilePrefab = tilePrefabComponent.PrefabEntity;

        CreateBoard();
    }

    private void CreateBoard()
{
    int rows = mapSettings.M;
    int columns = mapSettings.N;
    int colorCount = mapSettings.K;
    List<int2> obstaclePositionsCache = mapSettings.ObstaclePositions;
    // Grid ve GridEntities için NativeArray kullanımı
    var grid = new NativeArray<int>(rows * columns, Allocator.Persistent);
    var gridEntities = new NativeArray<Entity>(rows * columns, Allocator.Persistent);

    var entityManager = EntityManager;
    SpriteArrayAuthoring colorSpriteManager = Object.FindFirstObjectByType<SpriteArrayAuthoring>();

    var obstaclePositions = obstaclePositionsCache;
    

    for (int row = 0; row < rows; row++)
    {
        for (int col = 0; col < columns; col++)
        {
            int index = row * columns + col;

            if (obstaclePositions.Contains(new int2(col, row)))
            {
                //Obstacle oluştur
                var obstacleEntity = entityManager.Instantiate(tilePrefab);
            
                //Grid'de obstacle olarak işaretle (-2)
                grid[index] = -2;
                gridEntities[index] = obstacleEntity;
               
                EntityManager.AddComponentData(obstacleEntity, new ObstacleData
                {
                    Health = 2, 
                    
                });
                entityManager.SetComponentData(obstacleEntity, new LocalTransform
                {
                    Position = new float3(col, row, 0),
                    Rotation = quaternion.identity,
                    Scale = 0.45f
                });
                if (entityManager.HasComponent<SpriteRenderer>(obstacleEntity))
                {
                    var spriteRenderer = entityManager.GetComponentObject<SpriteRenderer>(obstacleEntity);
                    spriteRenderer.sprite = colorSpriteManager.mappings[6].Sprites[0];
                    spriteRenderer.sortingOrder = row;
                }
            
                continue;
            }

            var tileEntity = entityManager.Instantiate(tilePrefab);
            grid[index] = UnityEngine.Random.Range(0, colorCount); 
            gridEntities[index] = tileEntity;

            entityManager.SetComponentData(tileEntity, new TileDataComponent
            {
                ColorIndex = grid[index] 
            });
            entityManager.SetComponentData(tileEntity, new LocalTransform
            {
                Position = new float3(col, row, 0),
                Rotation = quaternion.identity,
                Scale = 0.45f
            });

            if (entityManager.HasComponent<SpriteRenderer>(tileEntity))
            {
                var spriteRenderer = entityManager.GetComponentObject<SpriteRenderer>(tileEntity);
                spriteRenderer.sprite = colorSpriteManager.mappings[grid[index]].Sprites[0];
                spriteRenderer.sortingOrder = row;
            }
        }
    }

    var boardStateEntity = entityManager.CreateEntity(typeof(BoardState));
    entityManager.SetComponentData(boardStateEntity, new BoardState
    {
        Rows = rows,
        Columns = columns,
        Grid = grid,
        GridEntities = gridEntities
    });
}
    
    protected override void OnDestroy()
    {
        if (SystemAPI.HasSingleton<BoardState>())
        {
            var boardState = SystemAPI.GetSingleton<BoardState>();
            boardState.Grid.Dispose();
            boardState.GridEntities.Dispose();
        }
    }
}
