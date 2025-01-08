using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
partial struct WaveSpawnerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<WaveSpawning>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        RefRW<WaveSpawning> waveSpawning = SystemAPI.GetSingletonRW<WaveSpawning>();
        if (!waveSpawning.ValueRO.doSpawn)
            return;

        NativeArray<Entity> instiatedEntities = state.EntityManager.Instantiate(waveSpawning.ValueRO.prefab, waveSpawning.ValueRO.amountToSpawn, Allocator.Persistent);

        Random r = new ((uint)(waveSpawning.ValueRO.amountToSpawn + 1));
        for (int i = 0; i < waveSpawning.ValueRO.amountToSpawn; i++)
        {
            Entity entity = instiatedEntities[i];
            float3 facingDirection;
            if (!waveSpawning.ValueRO.isSphericalSpawn)
            {
                float2 facingDirection2D = r.NextFloat2Direction();
                facingDirection = new float3(facingDirection2D.x, 0, facingDirection2D.y);
            }
            else
            {
                facingDirection = r.NextFloat3Direction();
            }

            float length = r.NextFloat(waveSpawning.ValueRO.spawnRadiusMin, waveSpawning.ValueRO.spawnRadiusMax);
            float3 randomDistance = facingDirection * length;

            float3 worldPos = waveSpawning.ValueRO.spawnPosition + randomDistance;
            quaternion rotation = quaternion.LookRotationSafe(facingDirection, new float3(0, 1, 0));

            switch (waveSpawning.ValueRO.unitType)
            {
                case AttackableUnitType.Boid:
                    waveSpawning.ValueRW.currentNumberOfBoids++;
                    state.EntityManager.SetComponentData(entity, new Boid {
                        id = waveSpawning.ValueRO.currentNumberOfBoids,
                        velocity = facingDirection * (waveSpawning.ValueRO.boidSettings.minSpeed + waveSpawning.ValueRO.boidSettings.maxSpeed) / 2,
                        dmg = waveSpawning.ValueRO.boidSettings.dmg });
                    break;
                default:
                case AttackableUnitType.Drone:
                    if (math.all(waveSpawning.ValueRO.spawnPosition == waveSpawning.ValueRO.topSpawn))
                    {
                        state.EntityManager.SetComponentData(entity, new Drone
                        {
                            spawnPoint = SpawnPoint.Top
                        });
                    }
                    else if (math.all(waveSpawning.ValueRO.spawnPosition == waveSpawning.ValueRO.midSpawn))
                    {
                        state.EntityManager.SetComponentData(entity, new Drone
                        {
                            spawnPoint = SpawnPoint.Mid
                        });
                    }
                    else if (math.all(waveSpawning.ValueRO.spawnPosition == waveSpawning.ValueRO.botSpawn))
                    {
                        state.EntityManager.SetComponentData(entity, new Drone
                        {
                            spawnPoint = SpawnPoint.Bot
                        });
                    }
                    state.EntityManager.GetBuffer<PathPositions>(entity).Add(new PathPositions() { pos = worldPos });
                    break;
                case AttackableUnitType.Tank:
                    rotation = quaternion.LookRotation(new float3(1, 0, 0), new float3(0, 1, 0));
                    break;
            }

            state.EntityManager.SetComponentData(entity, new LocalTransform
            {
                Position = worldPos,
                Rotation = rotation,
                Scale = waveSpawning.ValueRO.unitSize
            });

            waveSpawning.ValueRW.doSpawn = false;
        }

        instiatedEntities.Dispose();
    }
}
