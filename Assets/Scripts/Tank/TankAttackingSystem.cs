using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
public partial class TankAttackingSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem beginFixedStepSimulationEcbSystem;

    [BurstCompile]
    protected override void OnUpdate()
    {
        beginFixedStepSimulationEcbSystem = World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
        EntityCommandBuffer ecb = beginFixedStepSimulationEcbSystem.CreateCommandBuffer();

        CountEnemies(out NativeArray<Entity> entityEnemyArray);

        TankAttackingJob tankAttackingJob = new()
        {
            ecbParallelWriter = ecb.AsParallelWriter(),
            allAttackables = GetComponentLookup<Attackable>(),
            allLocalTransforms = GetComponentLookup<LocalTransform>(),
            allEntityEnemies = entityEnemyArray,
            deltaTime = SystemAPI.Time.DeltaTime
        };
        Dependency = tankAttackingJob.ScheduleParallel(Dependency);
        beginFixedStepSimulationEcbSystem.AddJobHandleForProducer(Dependency);
    }

    [BurstCompile]
    private bool CountEnemies(out NativeArray<Entity> pEntityEnemyArray)
    {
        EntityQueryDesc entityQueryDesc = new ()
        {
            All = new ComponentType[] { typeof(Attackable), typeof(LocalTransform) },
            Any = new ComponentType[] { typeof(Drone) }
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