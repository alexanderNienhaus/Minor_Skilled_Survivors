using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public partial class BoidTargetSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<BoidTarget>();
    }

    protected override void OnUpdate()
    {
        foreach ((RefRW<Boid> boid, RefRO<LocalTransform> localTransformBoid)
            in SystemAPI.Query<RefRW<Boid>, RefRO<LocalTransform>>())
        {
            if (EntityManager.Exists(boid.ValueRO.target))
                continue;

            boid.ValueRW.targetPosition = float3.zero;

            BoidTargetJob findTargetJob = new()
            {
                boid = boid,
                localTransformBoid = localTransformBoid.ValueRO
            };
            Dependency = findTargetJob.ScheduleParallel(Dependency);
            Dependency.Complete();
        }

        ComponentLookup<LocalTransform> allLocalTransforms = GetComponentLookup<LocalTransform>(true);
        foreach (RefRW<Boid> boid in SystemAPI.Query<RefRW<Boid>>())
        {
            if (!EntityManager.Exists(boid.ValueRO.target) || !allLocalTransforms.HasComponent(boid.ValueRO.target))
                continue;
            
            LocalTransform lt = allLocalTransforms[boid.ValueRO.target];
            boid.ValueRW.targetPosition = lt.Position + new float3(0, 1.5f, 0);
        }
    }
}
