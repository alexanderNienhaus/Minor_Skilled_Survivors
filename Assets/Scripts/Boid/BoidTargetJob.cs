using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;

[BurstCompile]
//[WithAll(typeof(BoidTarget))]
public partial struct BoidTargetJob : IJobEntity
{
    [NativeDisableContainerSafetyRestriction] public EntityManager em;
    [NativeDisableContainerSafetyRestriction] [ReadOnly] public ComponentLookup<LocalTransform> allLocalTransforms;
    [NativeDisableContainerSafetyRestriction] public ComponentLookup<Attackable> allAttackables;
    [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> allUnitEntities;

    [BurstCompile]
    public void Execute(ref Boid pBoid, ref LocalTransform pLocalTransformBoid)
    {
        if (em.Exists(pBoid.target))
            return;

        float shortestDistanceSq = float.MaxValue;
        Entity nearestTarget = Entity.Null;
        float3 nearestTargetLocalTransform = float3.zero;
        Attackable nearestTargetAttackable = default;

        for (int i = 0; i < allUnitEntities.Length; i++)
        {
            Entity unitEntity = allUnitEntities[i];
            LocalTransform localTransformUnit = allLocalTransforms[unitEntity];
            Attackable attackableUnit = allAttackables[unitEntity];

            float distanceSq = math.lengthsq(localTransformUnit.Position - pLocalTransformBoid.Position);
            if (shortestDistanceSq > distanceSq)
            {
                nearestTarget = unitEntity;
                nearestTargetLocalTransform = localTransformUnit.Position;
                nearestTargetAttackable = attackableUnit;
                shortestDistanceSq = distanceSq;
            }
        }

        if(shortestDistanceSq < float.MaxValue)
        {
            pBoid.target = nearestTarget;
            pBoid.targetPosition = nearestTargetLocalTransform + nearestTargetAttackable.halfBounds;
        }
    }
    
    /*
    [NativeDisableUnsafePtrRestriction] public RefRW<Boid> boid;
    public LocalTransform localTransformBoid;
    [NativeDisableContainerSafetyRestriction] public EntityManager em;

    [BurstCompile]
    public void Execute(ref LocalTransform localTransformTarget, in Attackable attackable, Entity entity)
    {
        if (!em.Exists(entity) || (!math.all(boid.ValueRW.targetPosition == float3.zero)
            && math.lengthsq(boid.ValueRW.targetPosition - localTransformBoid.Position) <= math.lengthsq(localTransformTarget.Position - localTransformBoid.Position)))
            return;

        boid.ValueRW.target = entity;
        boid.ValueRW.targetPosition = localTransformTarget.Position + attackable.halfBounds;
    }
    */
}
