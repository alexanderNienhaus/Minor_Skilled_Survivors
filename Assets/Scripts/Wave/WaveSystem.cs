using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[UpdateAfter(typeof(RegisterMapLayoutSystem))]
public partial class WaveSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem beginFixedStepSimulationEcbSystem;
    private EntityCommandBuffer ecb;
    private DynamicBuffer<Spawn> spawns;
    private TimerSystem timerSystem;
    private bool isActive;
    private int currentWaveNumber;
    private float currentWaveTime;
    private float currentWaveTolerance;
    private int currentSpawnNumber;
    private BoidSettings boidSettings;
    private bool lastWave;
    private NativeList<PathPositions> topPath;
    private NativeList<PathPositions> midPath;
    private NativeList<PathPositions> botPath;
    private float3 topSpawn;
    private float3 midSpawn;
    private float3 botSpawn;
    private bool doOnce;
    private bool spawning;
    private int currentNumberOfBoids;
    private int totalNumberOfEnemies;
    private int totalSpawnedNumber;

    protected override void OnCreate()
    {
        RequireForUpdate<Wave>();
        RequireForUpdate<Spawn>();
        RequireForUpdate<BoidSettings>();
        beginFixedStepSimulationEcbSystem = World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
        currentWaveNumber = 0;
        currentWaveTolerance = 0.05f;
        totalNumberOfEnemies = 0;
        totalSpawnedNumber = 0;
        lastWave = false;
        isActive = false;
        doOnce = false;
        spawning = false;
    }

    protected override void OnUpdate()
    {
        EventBus<OnWaveNumberChangedEvent>.Publish(new OnWaveNumberChangedEvent(currentWaveNumber));
        spawns = SystemAPI.GetSingletonBuffer<Spawn>();

        if (!doOnce)
        {
            DronePathFindingSystem dronePathFindingSystem = World.GetExistingSystemManaged<DronePathFindingSystem>();
            topSpawn = new float3(175, 0, -185);
            midSpawn = new float3(175, 0, 0);
            botSpawn = new float3(175, 0, 185);
            topPath = dronePathFindingSystem.FindPath(topSpawn + new float3(-25, 0, 0));
            midPath = dronePathFindingSystem.FindPath(midSpawn + new float3(-25, 0, 0));
            botPath = dronePathFindingSystem.FindPath(botSpawn + new float3(-25, 0, 0));
            doOnce = true;
            foreach (Spawn spawn in spawns)
            {
                totalNumberOfEnemies += spawn.amountToSpawn;
            }
        }

        if (!isActive)
            return;

        currentWaveTime += SystemAPI.Time.DeltaTime;

        timerSystem = World.GetExistingSystemManaged<TimerSystem>();
        boidSettings = SystemAPI.GetSingleton<BoidSettings>();

        SpawnFromWaves();

        if (CheckForWaveEnd())
            return;
    }

    private bool CheckForWaveEnd()
    {
        EntityQueryDesc entityQueryDesc = new EntityQueryDesc
        {
            Any = new ComponentType[] { typeof(Boid), typeof(Drone) }, 
        };
        int numberOfEnemies = GetEntityQuery(entityQueryDesc).CalculateEntityCount();

        //Debug.Log("Fighting Phase: " + currentWaveTime + " numberOfEnemies: " + numberOfEnemies + " currentSpawnNumber: " + currentSpawnNumber + " currentWaveNumber: " + currentWaveNumber);

        if (numberOfEnemies <= 0 && currentSpawnNumber > 1 && !spawning)
        {
            isActive = false;
            timerSystem.BuildPhaseStart();
            if (lastWave)
            {
                EventBus<OnEndGameEvent>.Publish(new OnEndGameEvent(true));
            }
            return true;
        }
        return false;
    }

    private void SpawnFromWaves()
    {
        spawning = false;
        foreach (Spawn spawn in spawns)
        {
            if (spawn.waveNumber != currentWaveNumber || currentSpawnNumber != spawn.spawnNumber ||
                !(spawn.whenToSpawn > currentWaveTime - currentWaveTolerance && spawn.whenToSpawn <= currentWaveTime + currentWaveTolerance))
                continue;

            Spawn(spawn.prefab, spawn.spawnPosition, spawn.amountToSpawn, spawn.unitType, spawn.unitSize, spawn.spawnRadiusMin,
                spawn.spawnRadiusMax, spawn.isSphericalSpawn);

            spawning = true;
            currentSpawnNumber++;
            totalSpawnedNumber += spawn.amountToSpawn;

            if (totalSpawnedNumber >= totalNumberOfEnemies)
            {
                lastWave = true;
            }
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
        float spawnRadiusMin, float spawnRadiusMax, bool isSphericalSpawn)
    {
        ecb = beginFixedStepSimulationEcbSystem.CreateCommandBuffer();

        //Debug.Log("SPAWN");
        Random r = new Random((uint)(amountToSpawn + 1));
        float3 boidStartSpeed = (boidSettings.minSpeed + boidSettings.maxSpeed) / 2;
        for (int i = 0; i < amountToSpawn; i++)
        {
            float3 facingDirection;
            if (!isSphericalSpawn)
            {
                float2 facingDirection2D = r.NextFloat2Direction();
                facingDirection = new float3(facingDirection2D.x, 0, facingDirection2D.y);
            }
            else
            {
                facingDirection = r.NextFloat3Direction();
            }

            float length = r.NextFloat(spawnRadiusMin, spawnRadiusMax);
            float3 randomDistance = facingDirection * length;

            float3 worldPos = spawnPosition + randomDistance;
            quaternion rotation = quaternion.LookRotation(facingDirection, new float3(0, 1, 0));
            Entity entity = ecb.Instantiate(prefab);

            //ecb.AddComponent<Parent>(entity);
            //ecb.SetComponent(entity, new Parent { Value = spawn.parent });

            switch (unitType)
            {
                case AttackableUnitType.Boid:
                    currentNumberOfBoids++;
                    ecb.SetComponent(entity, new Boid { id = currentNumberOfBoids, velocity = facingDirection * boidStartSpeed, dmg = boidSettings.dmg });
                    break;
                default:
                case AttackableUnitType.Drone:
                    if (math.all(spawnPosition == topSpawn))
                    {
                        foreach (PathPositions pathPositions in topPath)
                        {
                            ecb.AppendToBuffer(entity, pathPositions);
                        }
                    }
                    else if (math.all(spawnPosition == midSpawn))
                    {
                        foreach (PathPositions pathPositions in midPath)
                        {
                            ecb.AppendToBuffer(entity, pathPositions);
                        }
                    }
                    else if (math.all(spawnPosition == botSpawn))
                    {
                        foreach (PathPositions pathPositions in botPath)
                        {
                            ecb.AppendToBuffer(entity, pathPositions);
                        }
                    }                    
                    ecb.AppendToBuffer(entity, new PathPositions() { pos = worldPos });
                    break;
                case AttackableUnitType.Tank:
                    rotation = quaternion.LookRotation(new float3(1, 0, 0), new float3(0, 1, 0));
                    break;
            }

            ecb.SetComponent(entity, new LocalTransform
            {
                Position = worldPos,
                Rotation = rotation,
                Scale = unitSize
            });
        }
    }
}