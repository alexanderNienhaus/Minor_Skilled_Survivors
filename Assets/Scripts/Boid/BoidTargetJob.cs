using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;

[BurstCompile]
[WithAll(typeof(Boid))]
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial struct BoidTargetJob : IJobEntity
{
    [NativeDisableContainerSafetyRestriction] public EntityManager em;
    public EntityCommandBuffer.ParallelWriter ecbParallelWriter;
    [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> allTargetEntities;
    [NativeDisableContainerSafetyRestriction] [ReadOnly] public ComponentLookup<LocalTransform> allLocalTransforms;
    [NativeDisableContainerSafetyRestriction] public ComponentLookup<Attackable> allAttackables;
    public float deltaTime;

    [BurstCompile]
    public void Execute(ref Boid pBoid, ref LocalTransform pLocalTransformBoid, in Entity pBoidEntity, [ChunkIndexInQuery] int pChunkIndexInQuery)
    {
        if (em.Exists(pBoid.target))
        {
            pBoid.targetPosition = allLocalTransforms[pBoid.target].Position;
            if (math.length(pBoid.targetPosition - pLocalTransformBoid.Position) > pBoid.strikeDistance)
                return;

            int dmg = pBoid.dmg * (allAttackables[pBoid.target].attackableUnitType == AttackableUnitType.Tank ? 10 : 1);
            allAttackables.GetRefRW(pBoid.target).ValueRW.currentHp -= dmg;
            ecbParallelWriter.DestroyEntity(pChunkIndexInQuery, pBoidEntity);

            if (allAttackables[pBoid.target].currentHp - dmg > 0 || allAttackables[pBoid.target].attackableUnitType == AttackableUnitType.Base)
                return;

            ecbParallelWriter.DestroyEntity(pChunkIndexInQuery, pBoid.target);
            return;
        }

        Entity closestTargetEntity = Entity.Null;
        float shortestDistanceSq = float.MaxValue;
        for (int i = 0; i < allTargetEntities.Length; i++)
        {
            float distanceSq = math.lengthsq(allLocalTransforms[allTargetEntities[i]].Position - pLocalTransformBoid.Position);
            if (distanceSq > shortestDistanceSq)
                continue;

            shortestDistanceSq = distanceSq;
            closestTargetEntity = allTargetEntities[i];
        }

        if (closestTargetEntity == Entity.Null)
            return;

        LocalTransform localTransformTarget = allLocalTransforms[closestTargetEntity];
        Attackable attackableTarget = allAttackables[closestTargetEntity];

        pBoid.target = closestTargetEntity;
        float3 targetPos = localTransformTarget.Position + attackableTarget.halfBounds;
        pBoid.targetPosition = targetPos;
    }
}
