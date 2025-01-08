using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct BoidTargetSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState pSystemState)
    {
        pSystemState.RequireForUpdate<BoidTarget>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState pSystemState)
    {
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
        
    }
}
