using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public partial class WaveSystem : SystemBase
{
    public int currentNumberOfBoids;

    private EndFixedStepSimulationEntityCommandBufferSystem beginFixedStepSimulationEcbSystem;
    private EntityCommandBuffer ecb;
    private bool isActive;
    private int currentWaveNumber;
    private float currentWaveTime;
    private float curerntWaveTolerance;
    private int currentSpawnNumber;
    private BoidSettings boidSettings;

    protected override void OnCreate()
    {
        RequireForUpdate<Spawn>();
        beginFixedStepSimulationEcbSystem = World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
        currentWaveNumber = 0;
        curerntWaveTolerance = 0.05f;
        //NextWave();
    }

    protected override void OnUpdate()
    {
        boidSettings = SystemAPI.GetSingleton<BoidSettings>();
        ecb = beginFixedStepSimulationEcbSystem.CreateCommandBuffer();
        if (!isActive)
            return;

        foreach (DynamicBuffer<Spawn> spawns in SystemAPI.Query<DynamicBuffer<Spawn>>())
        {
            foreach (Spawn spawn in spawns)
            {
                if (spawn.waveNumber != currentWaveNumber || currentSpawnNumber != spawn.spawnNumber ||
                    !(spawn.whenToSpawn > currentWaveTime - curerntWaveTolerance && spawn.whenToSpawn <= currentWaveTime + curerntWaveTolerance))
                    continue;

                Spawn(spawn.prefab, spawn.spawnPosition, spawn.amountToSpawn, spawn.unitType, spawn.unitSize, spawn.whenToSpawn, spawn.spawnRadiusMin,
                    spawn.spawnRadiusMax, spawn.isSphericalSpawn);
                currentSpawnNumber++;
            }
        }

        currentWaveTime += SystemAPI.Time.DeltaTime;
    }

    public void NextWave()
    {
        isActive = true;
        currentSpawnNumber = 1;
        currentWaveTime = 0;
        currentWaveNumber++;
        currentNumberOfBoids = 0;
    }

    public void Spawn(Entity prefab, float3 spawnPosition, int amountToSpawn, UnitType unitType, float unitSize,
        float whenToSpawn, float spawnRadiusMin, float spawnRadiusMax, bool isSphericalSpawn)
    {
        Random r = new Random((uint)(amountToSpawn + 1));
        float3 boidStartSpeed = (boidSettings.minSpeed + boidSettings.maxSpeed) / 2;
        for (int i = 0; i < amountToSpawn; i++)
        {
            float3 facingDirection = r.NextFloat3Direction();
            float3 randomDistance = facingDirection * r.NextFloat(spawnRadiusMin, spawnRadiusMax);
            if (!isSphericalSpawn)
            {
                randomDistance.y = 0;
            }
            float3 pos = spawnPosition + randomDistance;
            quaternion rotation = quaternion.LookRotation(facingDirection, new float3(0, 1, 0));
            Entity entity = ecb.Instantiate(prefab);

            //ecb.AddComponent<Parent>(entity);
            //ecb.SetComponent(entity, new Parent { Value = spawn.parent });



            switch (unitType)
            {
                case UnitType.Boid:
                    currentNumberOfBoids++;
                    ecb.SetComponent(entity, new Boid { id = currentNumberOfBoids, velocity = facingDirection * boidStartSpeed });
                    break;
                default:
                case UnitType.Drone:
                    break;
                case UnitType.Tank:
                    rotation = quaternion.LookRotation(new float3(1, 0, 0), new float3(0, 1, 0));
                    break;
            }

            ecb.SetComponent(entity, new LocalTransform
            {
                Position = pos,
                Rotation = rotation,
                Scale = unitSize
            });
        }
    }
}