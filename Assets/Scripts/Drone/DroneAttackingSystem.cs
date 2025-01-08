using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class DroneAttackingSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem beginSimulationEcbSystem;

    [BurstCompile]
    protected override void OnCreate()
    {
        RequireForUpdate<Drone>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        beginSimulationEcbSystem = World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
        EntityCommandBuffer ecb = beginSimulationEcbSystem.CreateCommandBuffer();

        GetUnitEntityArray(out NativeArray<Entity> entityUnitArray);

        DroneAttackingJob droneAttackingJob = new()
        {
            ecbParallelWriter = ecb.AsParallelWriter(),
            allAttackables = GetComponentLookup<Attackable>(),
            allLocalTransforms = GetComponentLookup<LocalTransform>(true),
            allUnitEntities = entityUnitArray,
            deltaTime = SystemAPI.Time.DeltaTime
        };
        Dependency = droneAttackingJob.ScheduleParallel(Dependency);
        beginSimulationEcbSystem.AddJobHandleForProducer(Dependency);
    }

    [BurstCompile]
    private void GetUnitEntityArray(out NativeArray<Entity> pEntityUnitArray)
    {
        EntityQueryBuilder entityQueryDesc = new(Allocator.Temp);
        entityQueryDesc.WithAll<Attackable, LocalTransform>().WithNone<Drone, Boid>();
        EntityQuery query = GetEntityQuery(entityQueryDesc);
        pEntityUnitArray = query.ToEntityArray(Allocator.Persistent);
        entityQueryDesc.Dispose();
    }
}