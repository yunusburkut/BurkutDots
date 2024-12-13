using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;

public partial class BoardInitializationSystem : SystemBase
{
    private Entity tilePrefab;

    protected override void OnUpdate()
    {
    }

    protected override void OnStartRunning()
    {
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
    int rows = 10, columns = 10, colorCount = 6;

    // Grid ve GridEntities için NativeArray kullanımı
    var grid = new NativeArray<int>(rows * columns, Allocator.Persistent);
    var gridEntities = new NativeArray<Entity>(rows * columns, Allocator.Persistent);

    var entityManager = EntityManager;
    SpriteArrayAuthoring colorSpriteManager = Object.FindFirstObjectByType<SpriteArrayAuthoring>();

    // Engel pozisyonları
    var obstaclePositions = new List<int2>
    {
        new int2(2, 2),
        new int2(4, 5),
        new int2(6, 7)
    };

    for (int row = 0; row < rows; row++)
    {
        for (int col = 0; col < columns; col++)
        {
            int index = row * columns + col;

            if (obstaclePositions.Contains(new int2(col, row)))
            {
                // Obstacle oluştur
                var obstacleEntity = entityManager.Instantiate(tilePrefab);
            
                // Grid'de engel olarak işaretle (-2)
                grid[index] = -2;
                gridEntities[index] = obstacleEntity;
               
                EntityManager.AddComponentData(obstacleEntity, new ObstacleData
                {
                    Health = 2, // Örneğin 2 can
                    
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

            // Normal taş oluştur
            var tileEntity = entityManager.Instantiate(tilePrefab);
            grid[index] = UnityEngine.Random.Range(0, colorCount); // Renk indeksini kaydet
            gridEntities[index] = tileEntity;

            entityManager.SetComponentData(tileEntity, new TileData
            {
                ColorIndex = grid[index] // Renk indeksini taş bileşenine aktar
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

    // BoardState Singleton bileşenini oluştur
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
