using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
public partial class TankAttackingSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem beginSimulationEcbSystem;
    private EntityQuery query;

    [BurstCompile]
    protected override void OnCreate()
    {
        RequireForUpdate<Resource>();
        RequireForUpdate<Tank>();

        EntityQueryBuilder entityQueryDesc = new(Allocator.Temp);
        entityQueryDesc.WithAll<Attackable, LocalTransform>().WithAny<Drone>();
        query = GetEntityQuery(entityQueryDesc);
        entityQueryDesc.Dispose();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        beginSimulationEcbSystem = World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
        EntityCommandBuffer ecb = beginSimulationEcbSystem.CreateCommandBuffer();

        TankAttackingJob tankAttackingJob = new()
        {
            ecbParallelWriter = ecb.AsParallelWriter(),
            allAttackables = GetComponentLookup<Attackable>(),
            allLocalTransforms = GetComponentLookup<LocalTransform>(true),
            allEntityEnemies = query.ToEntityArray(Allocator.TempJob),
            resource = SystemAPI.GetSingletonRW<Resource>(),
            deltaTime = SystemAPI.Time.DeltaTime
        };
        Dependency = tankAttackingJob.ScheduleParallel(Dependency);
        beginSimulationEcbSystem.AddJobHandleForProducer(Dependency);
    }
}