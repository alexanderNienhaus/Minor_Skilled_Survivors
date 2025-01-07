using Unity.Entities;
using Unity.Physics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

[BurstCompile]
public struct CollisionCheckJob : ITriggerEventsJob
{
    [ReadOnly] public ComponentLookup<SelectedUnitTag> allSelectedUnits;
    [ReadOnly] public ComponentLookup<SelectionVolumeTag> allSelectionVolumes;

    [ReadOnly] public ComponentLookup<Boid> allBoids;
    [NativeDisableContainerSafetyRestriction] public ComponentLookup<Attackable> allAttackables;

    public EntityCommandBuffer ecb;

    public void Execute(TriggerEvent pTriggerEvent)
    {
        Entity entityA = pTriggerEvent.EntityA;
        Entity entityB = pTriggerEvent.EntityB;

        if (!allSelectedUnits.HasComponent(entityA) || !allSelectedUnits.HasComponent(entityB))
        {
            if (allSelectedUnits.HasComponent(entityA) && allSelectionVolumes.HasComponent(entityB))
            {
                ecb.SetComponentEnabled<SelectedUnitTag>(entityA, true);
            }
            else if (allSelectedUnits.HasComponent(entityB) && allSelectionVolumes.HasComponent(entityA))
            {
                ecb.SetComponentEnabled<SelectedUnitTag>(entityB, true);
            }
        }

        if (!allBoids.HasComponent(entityA) || !allBoids.HasComponent(entityB))
        {
            if (allBoids.HasComponent(entityA) && allAttackables.HasComponent(entityB))
            {
                allAttackables.GetRefRW(entityB).ValueRW.currentHp -= allBoids.GetRefRO(entityA).ValueRO.dmg;
                /*
                if (allAttackables.GetRefRW(entityB).ValueRW.currentHp <= 0)
                {
                    ecb.DestroyEntity(entityB);
                }
                */
                ecb.DestroyEntity(entityA);
            }
            else if (allBoids.HasComponent(entityB) && allAttackables.HasComponent(entityA))
            {
                allAttackables.GetRefRW(entityA).ValueRW.currentHp -= allBoids.GetRefRO(entityB).ValueRO.dmg;
                /*
                if (allAttackables.GetRefRW(entityA).ValueRW.currentHp <= 0)
                {
                    ecb.DestroyEntity(entityA);
                }
                */
                ecb.DestroyEntity(entityB);
            }
        }
    }
}