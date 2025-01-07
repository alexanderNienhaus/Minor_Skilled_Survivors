using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;

[BurstCompile]
public partial struct RadioStationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        foreach ((RefRW<RadioStation> radioStation, RefRO<LocalTransform> localTransform)
            in SystemAPI.Query<RefRW<RadioStation>, RefRO<LocalTransform>>())
        {
            if (!radioStation.ValueRO.hasSpawned)
            {
                RefRW<WaveSpawning> waveSpawning = SystemAPI.GetSingletonRW<WaveSpawning>();

                radioStation.ValueRW.hasSpawned = true;

                waveSpawning.ValueRW.prefab = radioStation.ValueRO.prefab;
                waveSpawning.ValueRW.spawnPosition = localTransform.ValueRO.Position + radioStation.ValueRO.spawnPosition;
                waveSpawning.ValueRW.amountToSpawn = radioStation.ValueRO.amountToSpawn;
                waveSpawning.ValueRW.unitType = radioStation.ValueRO.unitType;
                waveSpawning.ValueRW.unitSize = radioStation.ValueRO.unitSize;
                waveSpawning.ValueRW.spawnRadiusMin = radioStation.ValueRO.spawnRadiusMin;
                waveSpawning.ValueRW.spawnRadiusMax = radioStation.ValueRO.spawnRadiusMax;
                waveSpawning.ValueRW.isSphericalSpawn = radioStation.ValueRO.isSphericalSpawn;
                waveSpawning.ValueRW.boidSettings = SystemAPI.GetSingleton<BoidSettings>();
                waveSpawning.ValueRW.doSpawn = true;
            }
        }
    }
}