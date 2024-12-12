
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
                movingTile.ElapsedTime += deltaTime;

                // Animasyon oranı (0 ile 1 arasında)
                float t = math.saturate(math.square(movingTile.ElapsedTime / movingTile.Duration));

                // Lineer interpolasyon (Lerp) ile pozisyonu güncelle
                transform.Position = math.lerp(movingTile.StartPosition, movingTile.EndPosition, t);
                // Animasyon tamamlandıysa hareket bileşenini kaldır
                if (t >= 1.0f)
                {
                    ecbParallel.RemoveComponent<MovingTileComponent>(entityInQueryIndex, entity); // ParallelWriter kullan
                    ecbParallel.RemoveComponent<Moving>(entityInQueryIndex, entity); // ParallelWriter kullan
                }
            }).ScheduleParallel();

        // İşlem tamamlandıktan sonra EntityCommandBuffer değişikliklerini uygula
        Dependency.Complete(); // Paralel işlerin tamamlanmasını bekle
        ecb.Playback(EntityManager); // Değişiklikleri uygula
        ecb.Dispose(); // Bellek temizliği
    }
}