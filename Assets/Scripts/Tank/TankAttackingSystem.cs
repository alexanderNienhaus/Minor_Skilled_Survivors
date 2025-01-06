using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
public partial class TankAttackingSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem beginFixedStepSimulationEcbSystem;

    protected override void OnCreate()
    {
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        beginFixedStepSimulationEcbSystem = World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
        EntityCommandBuffer ecb = beginFixedStepSimulationEcbSystem.CreateCommandBuffer();

        if (!CountEnemies(out NativeArray<Entity> entityEnemyArray))
            return;

        TankAttackingJob tankAttackingJob = new()
        {
            ecbParallelWriter = ecb.AsParallelWriter(),
            allAttackables = GetComponentLookup<Attackable>(),
            allLocalTransforms = GetComponentLookup<LocalTransform>(),
            ressource = SystemAPI.GetSingletonRW<Ressource>(),
            allEntityEnemies = entityEnemyArray,
            deltaTime = SystemAPI.Time.DeltaTime
        };
        Dependency = tankAttackingJob.ScheduleParallel(Dependency);
        beginFixedStepSimulationEcbSystem.AddJobHandleForProducer(Dependency);
    }

    [BurstCompile]
    private bool CountEnemies(out NativeArray<Entity> pEntityEnemyArray)
    {
        EntityQueryDesc entityQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(Attackable), typeof(LocalTransform), typeof(Drone) },
        };
        int count = GetEntityQuery(entityQueryDesc).CalculateEntityCount();

        int i = 0;
        pEntityEnemyArray = new NativeArray<Entity>(count, Allocator.Persistent);
        foreach ((RefRO<Attackable> boid, RefRO<LocalTransform> localTransform, Entity entity)
            in SystemAPI.Query<RefRO<Attackable>, RefRO<LocalTransform>>().WithEntityAccess().WithAll<Drone>())
        {
            pEntityEnemyArray[i] = entity;
            i++;
        }

        if (i == 0)
            return false;

        return true;
    }
}