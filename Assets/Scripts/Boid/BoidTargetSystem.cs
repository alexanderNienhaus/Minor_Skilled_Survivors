using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class BoidTargetSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem beginSimulationEcbSystem;

    [BurstCompile]
    protected override void OnUpdate()
    {
        beginSimulationEcbSystem = World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
        //EntityCommandBuffer ecb = new(Allocator.TempJob);
        EntityCommandBuffer ecb = beginSimulationEcbSystem.CreateCommandBuffer();

        GetUnitEntityArray(out NativeArray<Entity> entityUnitArray);

        BoidTargetJob boidTargetJob = new()
        {
            em = EntityManager,
            ecbParallelWriter = ecb.AsParallelWriter(),
            allAttackables = GetComponentLookup<Attackable>(),
            allLocalTransforms = GetComponentLookup<LocalTransform>(true),
            allTargetEntities = entityUnitArray,
            deltaTime = SystemAPI.Time.DeltaTime
        };
        Dependency = boidTargetJob.ScheduleParallel(Dependency);
        //Dependency.Complete();
        //ecb.Playback(EntityManager);
        //ecb.Dispose();
        beginSimulationEcbSystem.AddJobHandleForProducer(Dependency);

        /*
foreach ((RefRW<Boid> boid, RefRO<LocalTransform> localTransformBoid)
    in SystemAPI.Query<RefRW<Boid>, RefRO<LocalTransform>>())
{
    if (pSystemState.EntityManager.Exists(boid.ValueRO.target))
        continue;

    boid.ValueRW.targetPosition = float3.zero;

    BoidTargetJob findTargetJob = new()
    {
        boid = boid,
        localTransformBoid = localTransformBoid.ValueRO,
        em = pSystemState.EntityManager
    };
    pSystemState.Dependency = findTargetJob.ScheduleParallel(pSystemState.Dependency);
    pSystemState.Dependency.Complete();
}

ComponentLookup<LocalTransform> allLocalTransforms = pSystemState.GetComponentLookup<LocalTransform>(true);
foreach (RefRW<Boid> boid in SystemAPI.Query<RefRW<Boid>>())
{
    if (!pSystemState.EntityManager.Exists(boid.ValueRO.target) || !allLocalTransforms.HasComponent(boid.ValueRO.target))
        continue;

    boid.ValueRW.targetPosition = allLocalTransforms[boid.ValueRO.target].Position + new float3(0, 1.5f, 0);
}
*/
    }

    [BurstCompile]
    private void GetUnitEntityArray(out NativeArray<Entity> pEntityUnitArray)
    {
        EntityQueryBuilder entityQueryDesc = new(Allocator.Temp);
        entityQueryDesc.WithAll<Attackable, LocalTransform>().WithNone<Drone, Boid, AATurret>();
        EntityQuery query = GetEntityQuery(entityQueryDesc);
        pEntityUnitArray = query.ToEntityArray(Allocator.Persistent);
        entityQueryDesc.Dispose();
    }
}
