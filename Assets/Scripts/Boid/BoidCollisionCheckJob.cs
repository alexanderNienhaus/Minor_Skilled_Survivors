using Unity.Entities;
using Unity.Physics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

[BurstCompile]
public struct BoidCollisionCheckJob : ITriggerEventsJob
{
    public EntityCommandBuffer ecb;

    [ReadOnly] public ComponentLookup<Boid> allBoids;
    [NativeDisableContainerSafetyRestriction] public ComponentLookup<Attackable> allAttackables;

    [BurstCompile]
    public void Execute(TriggerEvent pTriggerEvent)
    {
        Entity entityA = pTriggerEvent.EntityA;
        Entity entityB = pTriggerEvent.EntityB;

        if (!allBoids.HasComponent(entityA) || !allBoids.HasComponent(entityB))
        {
            if (allBoids.HasComponent(entityA) && allAttackables.HasComponent(entityB))
            {
                int boidDmg = allBoids.GetRefRO(entityA).ValueRO.dmg;
                allAttackables.GetRefRW(entityB).ValueRW.currentHp -= boidDmg;
                ecb.DestroyEntity(entityA);

                //if (allAttackables[entityB].currentHp - boidDmg > 0)
                    //return;

                ecb.DestroyEntity(entityB);
            }
            else if (allBoids.HasComponent(entityB) && allAttackables.HasComponent(entityA))
            {
                int boidDmg = allBoids.GetRefRO(entityB).ValueRO.dmg;
                allAttackables.GetRefRW(entityA).ValueRW.currentHp -= allBoids.GetRefRO(entityB).ValueRO.dmg;
                ecb.DestroyEntity(entityB);

                //if (allAttackables[entityA].currentHp - boidDmg > 0)
                    //return;

                ecb.DestroyEntity(entityA);
            }
        }
    }
}