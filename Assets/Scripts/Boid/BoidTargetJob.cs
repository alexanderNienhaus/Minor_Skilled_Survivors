using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;

[BurstCompile]
public partial struct BoidTargetJob : IJobEntity
{
    [NativeDisableContainerSafetyRestriction] public EntityManager em;
    [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> allUnitEntities;
    [NativeDisableContainerSafetyRestriction] [ReadOnly] public ComponentLookup<LocalTransform> allLocalTransforms;
    [NativeDisableContainerSafetyRestriction] [ReadOnly] public ComponentLookup<Attackable> allAttackables;

    [BurstCompile]
    public void Execute(ref Boid pBoid, ref LocalTransform pLocalTransformBoid)
    {
        if (em.Exists(pBoid.target) && allLocalTransforms.HasComponent(pBoid.target) && allAttackables.HasComponent(pBoid.target))
        {
            pBoid.targetPosition = allLocalTransforms[pBoid.target].Position + allAttackables[pBoid.target].halfBounds;
            return;
        }

        pBoid.target = Entity.Null;
        pBoid.targetPosition = float3.zero;

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
            if (shortestDistanceSq <= distanceSq)
                continue;

            nearestTarget = unitEntity;
            nearestTargetLocalTransform = localTransformUnit.Position;
            nearestTargetAttackable = attackableUnit;
            shortestDistanceSq = distanceSq;
        }

        if (shortestDistanceSq >= float.MaxValue)
            return;

        pBoid.target = nearestTarget;
        pBoid.targetPosition = nearestTargetLocalTransform + nearestTargetAttackable.halfBounds;
    }
}
