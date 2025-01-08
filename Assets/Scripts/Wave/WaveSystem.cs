using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
[UpdateAfter(typeof(RegisterMapLayoutSystem))]
public partial class WaveSystem : SystemBase
{
    private DynamicBuffer<Spawn> spawns;
    private TimerSystem timerSystem;
    private bool isActive;
    private int currentWaveNumber;
    private float currentWaveTime;
    private float minWaveTime;
    private float waveTimeTolerance;
    private int currentSpawnNumber;
    private bool lastWave;
    private float3 topSpawn;
    private float3 midSpawn;
    private float3 botSpawn;
    private bool doOnce;
    private int totalNumberOfEnemies;
    private int totalSpawnedNumber;
    private NativeArray<int> numberOfEnemiesPerWave;
    private int totalSpawnedNumberThisWave;

    [BurstCompile]
    protected override void OnCreate()
    {
        RequireForUpdate<Wave>();
        RequireForUpdate<Spawn>();
        RequireForUpdate<BoidSettings>();
        currentWaveNumber = 0;
        waveTimeTolerance = 0.5f;
        totalNumberOfEnemies = 0;
        totalSpawnedNumber = 0;
        lastWave = false;
        isActive = false;
        doOnce = false;
        minWaveTime = 3;
        topSpawn = new float3(225, 2, -185);
        midSpawn = new float3(225, 2, 0);
        botSpawn = new float3(225, 2, 185);
        numberOfEnemiesPerWave = new NativeArray<int>(4, Allocator.Persistent);
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        EventBus<OnWaveNumberChangedEvent>.Publish(new OnWaveNumberChangedEvent(currentWaveNumber));
        spawns = SystemAPI.GetSingletonBuffer<Spawn>();

        if (!doOnce)
            DoOnce();

        if (!isActive)
            return;

        timerSystem = World.GetExistingSystemManaged<TimerSystem>();
        currentWaveTime += SystemAPI.Time.DeltaTime;
        if (CheckForWaveEnd())
            return;

        SpawnFromWaves();
    }

    private void DoOnce()
    {
        doOnce = true;
        int i = 0;
        foreach (Spawn spawn in spawns)
        {
            numberOfEnemiesPerWave[spawn.waveNumber - 1] += spawn.amountToSpawn;
            totalNumberOfEnemies += spawn.amountToSpawn;
            i++;
        }
    }

    [BurstCompile]
    private bool CheckForWaveEnd()
    {
        EntityQueryDesc entityQueryDesc = new()
        {
            Any = new ComponentType[] { typeof(Boid), typeof(Drone) },
        };
        int numberOfEnemies = GetEntityQuery(entityQueryDesc).CalculateEntityCount(); //Debug.Log("Fighting Phase: " + currentWaveTime + " numberOfEnemies: " + numberOfEnemies + " currentSpawnNumber: " + currentSpawnNumber + " currentWaveNumber: " + currentWaveNumber);

        if (numberOfEnemies > 0 || currentSpawnNumber <= 1 || currentWaveTime <= minWaveTime
            || totalSpawnedNumberThisWave < numberOfEnemiesPerWave[currentWaveNumber - 1])
            return false;

        isActive = false;
        timerSystem.BuildPhaseStart();

        if (!lastWave)
            return true;

        EventBus<OnEndGameEvent>.Publish(new OnEndGameEvent(true));
        return true;
    }

    [BurstCompile]
    private void SpawnFromWaves()
    {
        foreach (Spawn spawn in spawns)
        {
            if (spawn.waveNumber != currentWaveNumber || currentSpawnNumber != spawn.spawnNumber ||
                !(spawn.whenToSpawn > currentWaveTime - waveTimeTolerance && spawn.whenToSpawn <= currentWaveTime + waveTimeTolerance))
                continue;

            Spawn(spawn.prefab, spawn.spawnPosition, spawn.amountToSpawn, spawn.unitType, spawn.unitSize, spawn.spawnRadiusMin,
                spawn.spawnRadiusMax, spawn.isSphericalSpawn);

            currentSpawnNumber++;
            totalSpawnedNumberThisWave += spawn.amountToSpawn;
            totalSpawnedNumber += spawn.amountToSpawn;

            if (totalSpawnedNumber >= totalNumberOfEnemies)
            {
                lastWave = true;
            }
        }
    }

    [BurstCompile]
    public void NextWave()
    {
        isActive = true;
        currentSpawnNumber = 1;
        currentWaveTime = 0;
        totalSpawnedNumberThisWave = 0;
        currentWaveNumber++;
        foreach (RefRW<Attackable> attackable in SystemAPI.Query<RefRW<Attackable>>().WithAny<Tank, RadioStation, AATurret>())
        {
            attackable.ValueRW.currentHp = attackable.ValueRO.startHp;
        }
    }

    [BurstCompile]
    public void Spawn(Entity prefab, float3 spawnPosition, int amountToSpawn, AttackableUnitType unitType, float unitSize,
        float spawnRadiusMin, float spawnRadiusMax, bool isSphericalSpawn)
    {
        RefRW<WaveSpawning> waveSpawning = SystemAPI.GetSingletonRW<WaveSpawning>();

        waveSpawning.ValueRW.prefab = prefab;
        waveSpawning.ValueRW.spawnPosition = spawnPosition;
        waveSpawning.ValueRW.amountToSpawn = amountToSpawn;
        waveSpawning.ValueRW.unitType = unitType;
        waveSpawning.ValueRW.unitSize = unitSize;
        waveSpawning.ValueRW.spawnRadiusMin = spawnRadiusMin;
        waveSpawning.ValueRW.spawnRadiusMax = spawnRadiusMax;
        waveSpawning.ValueRW.isSphericalSpawn = isSphericalSpawn;
        waveSpawning.ValueRW.boidSettings = SystemAPI.GetSingleton<BoidSettings>();
        waveSpawning.ValueRW.topSpawn = topSpawn;
        waveSpawning.ValueRW.midSpawn = midSpawn;
        waveSpawning.ValueRW.botSpawn = botSpawn;
        waveSpawning.ValueRW.doSpawn = true;

        //World.GetExistingSystemManaged<AATurretAttackingSystem>().CountEnemies();
        //World.GetExistingSystemManaged<TankAttackingSystem>().CountEnemies();
    }
}