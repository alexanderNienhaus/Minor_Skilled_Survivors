using Unity.Entities;
using Unity.Physics;
using Unity.Burst;
using Unity.Collections;

[BurstCompile]
public struct MultipleUnitSelectionCheckJob : ITriggerEventsJob
{
    public EntityCommandBuffer ecb;

    [ReadOnly] public ComponentLookup<SelectedUnitTag> allSelectedUnits;
    [ReadOnly] public ComponentLookup<SelectionVolumeTag> allSelectionVolumes;

    [BurstCompile]
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
    }
}