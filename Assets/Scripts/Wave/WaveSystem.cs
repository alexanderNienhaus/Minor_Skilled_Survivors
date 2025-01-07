using Unity.Burst;
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

    [BurstCompile]
    protected override void OnCreate()
    {
        RequireForUpdate<Wave>();
        RequireForUpdate<Spawn>();
        RequireForUpdate<BoidSettings>();
        currentWaveNumber = 0;
        waveTimeTolerance = 0.01f;
        totalNumberOfEnemies = 0;
        totalSpawnedNumber = 0;
        lastWave = false;
        isActive = false;
        doOnce = false;
        minWaveTime = 3;
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        EventBus<OnWaveNumberChangedEvent>.Publish(new OnWaveNumberChangedEvent(currentWaveNumber));
        spawns = SystemAPI.GetSingletonBuffer<Spawn>();

        if (!doOnce)
        {
            doOnce = true;
            foreach (Spawn spawn in spawns)
            {
                totalNumberOfEnemies += spawn.amountToSpawn;
            }
        }

        if (!isActive)
            return;

        currentWaveTime += SystemAPI.Time.DeltaTime;
        if (CheckForWaveEnd())
            return;

        timerSystem = World.GetExistingSystemManaged<TimerSystem>();

        SpawnFromWaves();
    }

    [BurstCompile]
    private bool CheckForWaveEnd()
    {
        EntityQueryDesc entityQueryDesc = new EntityQueryDesc
        {
            Any = new ComponentType[] { typeof(Boid), typeof(Drone) }, 
        };
        int numberOfEnemies = GetEntityQuery(entityQueryDesc).CalculateEntityCount();

        //Debug.Log("Fighting Phase: " + currentWaveTime + " numberOfEnemies: " + numberOfEnemies + " currentSpawnNumber: " + currentSpawnNumber + " currentWaveNumber: " + currentWaveNumber);

        if (numberOfEnemies <= 0 && currentSpawnNumber > 1 && currentWaveTime > minWaveTime)
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
        currentWaveNumber++;
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