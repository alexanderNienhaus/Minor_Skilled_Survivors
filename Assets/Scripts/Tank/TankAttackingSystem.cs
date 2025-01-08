using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
//[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class TankAttackingSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem beginSimulationEcbSystem;
    private int enemyCount;

    [BurstCompile]
    protected override void OnCreate()
    {
        RequireForUpdate<Resource>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        beginSimulationEcbSystem = World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
        EntityCommandBuffer ecb = beginSimulationEcbSystem.CreateCommandBuffer();

        CountEnemies();
        GetEnemyEntityArray(out NativeArray<Entity> entityEnemyArray);

        TankAttackingJob tankAttackingJob = new()
        {
            ecbParallelWriter = ecb.AsParallelWriter(),
            em = EntityManager,
            allAttackables = GetComponentLookup<Attackable>(),
            allLocalTransforms = GetComponentLookup<LocalTransform>(),
            allEntityEnemies = entityEnemyArray,
            resource = SystemAPI.GetSingletonRW<Resource>(),
            deltaTime = SystemAPI.Time.DeltaTime
        };
        Dependency = tankAttackingJob.ScheduleParallel(Dependency);
        beginSimulationEcbSystem.AddJobHandleForProducer(Dependency);
    }

    [BurstCompile]
    public void CountEnemies()
    {
        EntityQueryDesc entityQueryDesc = new ()
        {
            All = new ComponentType[] { typeof(Attackable), typeof(LocalTransform) },
            Any = new ComponentType[] { typeof(Drone) }
        };
        enemyCount = GetEntityQuery(entityQueryDesc).CalculateEntityCount();
    }

    [BurstCompile]
    private void GetEnemyEntityArray(out NativeArray<Entity> pEntityEnemyArray)
    {
        int i = 0;
        pEntityEnemyArray = new NativeArray<Entity>(enemyCount, Allocator.Persistent);
        foreach ((RefRO<Attackable> boid, RefRO<LocalTransform> localTransform, Entity entity)
            in SystemAPI.Query<RefRO<Attackable>, RefRO<LocalTransform>>().WithEntityAccess().WithAll<Drone>())
        {
            if (!EntityManager.Exists(entity))
                continue;

            pEntityEnemyArray[i] = entity;
            i++;
        }
    }
}