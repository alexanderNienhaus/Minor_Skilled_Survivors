using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;
using Unity.Collections.LowLevel.Unsafe;

[BurstCompile]
[WithAll(typeof(BoidTarget))]
public partial struct BoidTargetJob : IJobEntity
{
    [NativeDisableUnsafePtrRestriction] public RefRW<Boid> boid;
    public LocalTransform localTransformBoid;
    [NativeDisableContainerSafetyRestriction] public EntityManager em;

    [BurstCompile]
    public void Execute(in LocalTransform localTransformTarget, in Attackable attackable, Entity entity)
    {
        if (!em.Exists(entity) || (!math.all(boid.ValueRW.targetPosition == float3.zero)
            && math.lengthsq(boid.ValueRW.targetPosition - localTransformBoid.Position) <= math.lengthsq(localTransformTarget.Position - localTransformBoid.Position)))
            return;

        boid.ValueRW.target = entity;
        boid.ValueRW.targetPosition = localTransformTarget.Position + attackable.halfBounds;
    }
}
