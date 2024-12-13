
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;


[BurstCompile]
public partial class TileMovementSystem : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireForUpdate<Moving>();
    }
    protected override void OnStopRunning()
    {
        World.DefaultGameObjectInjectionWorld
            .GetOrCreateSystemManaged<GroupDetectionSystem>().RunDetection();
    }
    [BurstCompile]
    protected override void OnUpdate()
    {
        float deltaTime = World.Time.DeltaTime;

        // EntityCommandBuffer oluştur
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);
        var ecbParallel = ecb.AsParallelWriter(); // ParallelWriter oluştur

        Entities
            .WithAll<Moving, MovingTileComponent, LocalTransform>()
            .ForEach((Entity entity, int entityInQueryIndex, ref MovingTileComponent movingTile,
                ref LocalTransform transform) =>
            {
                // Geçen zamanı güncelle
                float distance = math.abs(movingTile.StartPosition.y - movingTile.EndPosition.y);
                // Süreyi mesafeye göre hesapla
                // movingTile.Duration = distance / 10;
                // Animasyonu güncelle
                movingTile.ElapsedTime += deltaTime;
                // Animasyon oranı (0 ile 1 arasında)
                float t = math.saturate(movingTile.ElapsedTime / movingTile.Duration);

                // Lineer interpolasyon (Lerp) ile pozisyonu güncelle
                transform.Position = math.lerp(movingTile.StartPosition, movingTile.EndPosition, t);
               
                if (t >= 1.0f) //Animasyon tamamlandı demek
                {
                    ecbParallel.RemoveComponent<MovingTileComponent>(entityInQueryIndex, entity); 
                    ecbParallel.RemoveComponent<Moving>(entityInQueryIndex, entity); 
                }
            }).ScheduleParallel();

        
        Dependency.Complete(); // Paralel işlerin tamamlanmasını bekle
        ecb.Playback(EntityManager); // Değişiklikleri uygula
        ecb.Dispose(); // Bellek temizliği
    }
}