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
    private DynamicBuffer<Spawn> spawns;
    private DynamicBuffer<Wave> waves;
    private TimerSystem timerSystem;
    private bool isActive;
    private int currentWaveNumber;
    private float currentWaveTime;
    private float currentWaveTolerance;
    private int currentSpawnNumber;
    private BoidSettings boidSettings;

    protected override void OnCreate()
    {
        RequireForUpdate<Wave>();
        RequireForUpdate<Spawn>();
        RequireForUpdate<BoidSettings>();
        beginFixedStepSimulationEcbSystem = World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
        currentWaveNumber = 0;
        currentWaveTolerance = 0.05f;
        isActive = false;
    }

    protected override void OnUpdate()
    {
        if (!isActive)
            return;

        currentWaveTime += SystemAPI.Time.DeltaTime;

        timerSystem = World.GetExistingSystemManaged<TimerSystem>();

        if (CheckForWaveEnd())
            return;

        spawns = SystemAPI.GetSingletonBuffer<Spawn>();
        waves = SystemAPI.GetSingletonBuffer<Wave>();
        boidSettings = SystemAPI.GetSingleton<BoidSettings>();

        SpawnFromWaves();
    }

    private bool CheckForWaveEnd()
    {
        EntityQueryDesc entityQueryDesc = new EntityQueryDesc
        {
            Any = new ComponentType[] { typeof(Boid), typeof(Drone) }, 
        };
        int numberOfEnemies = GetEntityQuery(entityQueryDesc).CalculateEntityCount();

        //Debug.Log("Fighting Phase: " + currentWaveTime + " numberOfEnemies: " + numberOfEnemies + " currentSpawnNumber: " + currentSpawnNumber + " currentWaveNumber: " + currentWaveNumber);

        if (numberOfEnemies <= 0 && currentSpawnNumber > 1)
        {
            isActive = false;
            timerSystem.BuildPhaseStart();
            return true;
        }
        return false;
    }

    private void SpawnFromWaves()
    {
        foreach (Spawn spawn in spawns)
        {
            if (spawn.waveNumber != currentWaveNumber || currentSpawnNumber != spawn.spawnNumber ||
                !(spawn.whenToSpawn > currentWaveTime - currentWaveTolerance && spawn.whenToSpawn <= currentWaveTime + currentWaveTolerance))
                continue;

            Spawn(spawn.prefab, spawn.spawnPosition, spawn.amountToSpawn, spawn.unitType, spawn.unitSize, spawn.whenToSpawn, spawn.spawnRadiusMin,
                spawn.spawnRadiusMax, spawn.isSphericalSpawn);

            currentSpawnNumber++;
        }
    }

    public void NextWave()
    {
        isActive = true;
        currentSpawnNumber = 1;
        currentWaveTime = 0;
        currentWaveNumber++;
        currentNumberOfBoids = 0;
    }

    public void Spawn(Entity prefab, float3 spawnPosition, int amountToSpawn, AttackableUnitType unitType, float unitSize,
        float whenToSpawn, float spawnRadiusMin, float spawnRadiusMax, bool isSphericalSpawn)
    {
        ecb = beginFixedStepSimulationEcbSystem.CreateCommandBuffer();

        //Debug.Log("SPAWN");
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
                case AttackableUnitType.Boid:
                    currentNumberOfBoids++;
                    ecb.SetComponent(entity, new Boid { id = currentNumberOfBoids, velocity = facingDirection * boidStartSpeed });
                    break;
                default:
                case AttackableUnitType.Drone:
                    break;
                case AttackableUnitType.Tank:
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