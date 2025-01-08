using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
public partial class TankAttackingSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem beginSimulationEcbSystem;

    [BurstCompile]
    protected override void OnCreate()
    {
        RequireForUpdate<Resource>();
        RequireForUpdate<Tank>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        beginSimulationEcbSystem = World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
        EntityCommandBuffer ecb = beginSimulationEcbSystem.CreateCommandBuffer();

        GetEnemyEntityArray(out NativeArray<Entity> entityEnemyArray);

        TankAttackingJob tankAttackingJob = new()
        {
            ecbParallelWriter = ecb.AsParallelWriter(),
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
    private void GetEnemyEntityArray(out NativeArray<Entity> pEntityEnemyArray)
    {
        EntityQueryBuilder entityQueryDesc = new(Allocator.Temp);
        entityQueryDesc.WithAll<Attackable, LocalTransform>().WithAny<Drone>();
        EntityQuery query = GetEntityQuery(entityQueryDesc);
        pEntityEnemyArray = query.ToEntityArray(Allocator.Persistent);
        entityQueryDesc.Dispose();
    }
}