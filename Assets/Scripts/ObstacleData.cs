using Unity.Entities;

public partial struct ObstacleData : IComponentData
{
    public int Health; // Obstacle'ın mevcut canı
    public int MaxHealth; // Obstacle'ın başlangıçtaki maksimum canı
}