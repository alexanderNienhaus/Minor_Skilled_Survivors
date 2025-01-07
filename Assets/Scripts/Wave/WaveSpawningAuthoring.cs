using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public class WaveSpawningAuthoring : MonoBehaviour
{
    private class Baker : Baker<WaveSpawningAuthoring>
    {
        public override void Bake(WaveSpawningAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new WaveSpawning()
            {
                currentNumberOfBoids = 0
            });
        }
    }
}

[BurstCompile]
public struct WaveSpawning : IComponentData
{
    public bool doSpawn;
    public Entity prefab;
    public float3 spawnPosition;
    public int amountToSpawn;
    public AttackableUnitType unitType;
    public float unitSize;
    public float spawnRadiusMin;
    public float spawnRadiusMax;
    public bool isSphericalSpawn;
    public BoidSettings boidSettings;
    public float3 topSpawn;
    public float3 midSpawn;
    public float3 botSpawn;
    public int currentNumberOfBoids;
}
