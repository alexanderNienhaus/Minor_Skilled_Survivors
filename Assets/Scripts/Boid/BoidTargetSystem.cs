using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

[BurstCompile]
public partial struct BoidTargetSystem : ISystem
{
    private EntityQuery query;
    private ComponentLookup<Attackable> allAttackables;
    [ReadOnly] private ComponentLookup<LocalTransform> allLocaltransforms;

    [BurstCompile]
    public void OnCreate(ref SystemState pSystemState)
    {
        pSystemState.RequireForUpdate<BoidTarget>();
        pSystemState.RequireForUpdate<Boid>();

        EntityQueryBuilder entityQueryDesc = new(Allocator.Temp);
        entityQueryDesc.WithAll<Attackable, LocalTransform>().WithNone<Drone, Boid, AATurret>();
        query = pSystemState.GetEntityQuery(entityQueryDesc);
        entityQueryDesc.Dispose();

        allAttackables = pSystemState.GetComponentLookup<Attackable>();
        allLocaltransforms = pSystemState.GetComponentLookup<LocalTransform>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState pSystemState)
    {
        allAttackables.Update(ref pSystemState);
        allLocaltransforms.Update(ref pSystemState);

        BoidTargetJob boidTargetJob = new()
        {
            em = pSystemState.EntityManager,
            allAttackables = allAttackables,
            allLocalTransforms = allLocaltransforms,
            allUnitEntities = query.ToEntityArray(Allocator.TempJob)
        };

        pSystemState.Dependency = boidTargetJob.ScheduleParallel(pSystemState.Dependency);
    }
}
