using Unity.Entities;
using Unity.Physics;
using Unity.Burst;
using Unity.Collections;

[BurstCompile]
public struct MultipleUnitSelectionCheckJob : ITriggerEventsJob
{
    public EntityCommandBuffer ecb;

    [ReadOnly] public ComponentLookup<SelectedUnitTag> allSelectedUnitTags;
    [ReadOnly] public ComponentLookup<SelectionVolumeTag> allSelectionVolumeTags;

    [BurstCompile]
    public void Execute(TriggerEvent pTriggerEvent)
    {
        Entity entityA = pTriggerEvent.EntityA;
        Entity entityB = pTriggerEvent.EntityB;

        if (!allSelectedUnitTags.HasComponent(entityA) || !allSelectedUnitTags.HasComponent(entityB))
        {
            if (allSelectedUnitTags.HasComponent(entityA) && allSelectionVolumeTags.HasComponent(entityB))
            {
                ecb.SetComponentEnabled<SelectedUnitTag>(entityA, true);
            }
            else if (allSelectedUnitTags.HasComponent(entityB) && allSelectionVolumeTags.HasComponent(entityA))
            {
                ecb.SetComponentEnabled<SelectedUnitTag>(entityB, true);
            }
        }
    }
}