using Unity.Entities;
using Unity.Physics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

[BurstCompile]
public struct BoidCollisionCheckJob : ITriggerEventsJob
{
    [NativeDisableContainerSafetyRestriction] public EntityManager em;
    public EntityCommandBuffer ecb;

    [ReadOnly] public ComponentLookup<Boid> allBoids;
    [NativeDisableContainerSafetyRestriction] public ComponentLookup<Attackable> allAttackables;

    [BurstCompile]
    public void Execute(TriggerEvent pTriggerEvent)
    {
        if (!em.Exists(pTriggerEvent.EntityA) || !em.Exists(pTriggerEvent.EntityB))
            return;

        Entity entityA = pTriggerEvent.EntityA;
        Entity entityB = pTriggerEvent.EntityB;

        if (!allBoids.HasComponent(entityA) || !allBoids.HasComponent(entityB))
        {
            if (allBoids.HasComponent(entityA) && allAttackables.HasComponent(entityB))
            {
                int dmg = allBoids.GetRefRO(entityA).ValueRO.dmg *
                    (allAttackables.GetRefRO(entityA).ValueRO.attackableUnitType == AttackableUnitType.Tank ? 10 : 1);
                allAttackables.GetRefRW(entityB).ValueRW.currentHp -= dmg;
                ecb.DestroyEntity(entityA);

                if (allAttackables[entityB].currentHp - dmg > 0)
                    return;

                ecb.DestroyEntity(entityB);
            }
            else if (allBoids.HasComponent(entityB) && allAttackables.HasComponent(entityA))
            {
                int dmg = allBoids.GetRefRO(entityB).ValueRO.dmg *
                    (allAttackables.GetRefRO(entityA).ValueRO.attackableUnitType == AttackableUnitType.Tank ? 10 : 1);
                allAttackables.GetRefRW(entityA).ValueRW.currentHp -= dmg;
                ecb.DestroyEntity(entityB);

                if (allAttackables[entityA].currentHp - dmg > 0)
                    return;

                ecb.DestroyEntity(entityA);
            }
        }
    }
}