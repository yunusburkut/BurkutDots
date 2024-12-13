
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
        var ecbParallel = ecb.AsParallelWriter(); // Paralel işlem yapmak için parallelwriter oluşturuyoruz 

        Entities
            .WithAll<Moving, MovingTileComponent, LocalTransform>()
            .ForEach((Entity entity, int entityInQueryIndex, ref MovingTileComponent movingTile,
                ref LocalTransform transform) =>
            {
                float distance = math.abs(movingTile.StartPosition.y - movingTile.EndPosition.y);
                // Süreyi mesafeye göre hesaplaadım her tile aynı hızda düşsün diye ama garip bi hata iile karşılaştıgım için commentledim
                // movingTile.Duration = distance / 10;
                movingTile.ElapsedTime += deltaTime;
                float t = math.saturate(movingTile.ElapsedTime / movingTile.Duration);
                transform.Position = math.lerp(movingTile.StartPosition, movingTile.EndPosition, t);
               
                if (t >= 1.0f) //Ulaşmasını istedigimiz süre tamamladı "Animasyon" tamamladı check'i
                {
                    ecbParallel.RemoveComponent<MovingTileComponent>(entityInQueryIndex, entity); 
                    ecbParallel.RemoveComponent<Moving>(entityInQueryIndex, entity); 
                }
            }).ScheduleParallel();

        
        Dependency.Complete(); // Paralel işlerin tamamlanmasını bekliyoruz
        ecb.Playback(EntityManager); // Değişiklikleri uyguluyoruz
        ecb.Dispose(); // Olası bi leak önlemek için bellek temizliyoruz
    }
}