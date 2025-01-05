using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;

[BurstCompile]
[CreateAfter(typeof(WaveSystem))]
[UpdateAfter(typeof(WaveSystem))]
public partial class RadioStationSystem : SystemBase
{
    private WaveSystem waveSystem;

    protected override void OnCreate()
    {
        waveSystem = World.GetExistingSystemManaged<WaveSystem>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        foreach ((RefRW<RadioStation> radioStation, RefRO<LocalTransform> localTransform)
            in SystemAPI.Query<RefRW<RadioStation>, RefRO<LocalTransform>>())
        {
            if (!radioStation.ValueRO.hasSpawned)
            {
                waveSystem.Spawn(radioStation.ValueRO.prefab, localTransform.ValueRO.Position + radioStation.ValueRO.spawnPosition,
                    radioStation.ValueRO.amountToSpawn, radioStation.ValueRO.unitType, radioStation.ValueRO.unitSize, radioStation.ValueRO.whenToSpawn,
                    radioStation.ValueRO.spawnRadiusMin, radioStation.ValueRO.spawnRadiusMax, radioStation.ValueRO.isSphericalSpawn);
                radioStation.ValueRW.hasSpawned = true;
            }
        }
    }
}