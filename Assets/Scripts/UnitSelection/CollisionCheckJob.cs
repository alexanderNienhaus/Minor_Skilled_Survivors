using Unity.Entities;
using Unity.Physics;
using Unity.Burst;
using Unity.Collections;

[BurstCompile]
public struct CollisionCheckJob : ITriggerEventsJob
{
    [ReadOnly] public ComponentLookup<SelectedUnitTag> allSelectedUnits;
    [ReadOnly] public ComponentLookup<SelectionVolumeTag> allSelectionVolumes;
    [ReadOnly] public ComponentLookup<Boid> allBoids;
    [ReadOnly] public ComponentLookup<AttackableUnit> allAttackableUnits;
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
            if (allBoids.HasComponent(entityA) && allAttackableUnits.HasComponent(entityB))
            {
                ecb.DestroyEntity(entityA);
                ecb.DestroyEntity(entityB);
            }
            else if (allBoids.HasComponent(entityB) && allAttackableUnits.HasComponent(entityA))
            {
                ecb.DestroyEntity(entityA);
                ecb.DestroyEntity(entityB);
            }
        }
    }
}