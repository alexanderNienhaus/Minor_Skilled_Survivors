using Unity.Entities;
using Unity.Physics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct MultipleUnitSelectionCheckSystem : ISystem
{
    [ReadOnly] private ComponentLookup<SelectedUnitTag> allSelectedUnitTags;
    [ReadOnly] private ComponentLookup<SelectionVolumeTag> allSelectionVolumeTags;

    [BurstCompile]
    public void OnCreate(ref SystemState pSystemState)
    {
        pSystemState.RequireForUpdate<SelectedUnitTag>();
        pSystemState.RequireForUpdate<SelectionVolumeTag>();

        allSelectedUnitTags = pSystemState.GetComponentLookup<SelectedUnitTag>(true);
        allSelectionVolumeTags = pSystemState.GetComponentLookup<SelectionVolumeTag>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState pSystemState)
    {
        EntityCommandBuffer ecb = new (Allocator.TempJob);

        allSelectedUnitTags.Update(ref pSystemState);
        allSelectionVolumeTags.Update(ref pSystemState);

        MultipleUnitSelectionCheckJob triggerJob = new ()
        {
            allSelectedUnitTags = allSelectedUnitTags,
            allSelectionVolumeTags = allSelectionVolumeTags,
            ecb = ecb
        };

        pSystemState.Dependency = triggerJob.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), pSystemState.Dependency);
        pSystemState.Dependency.Complete();
        ecb.Playback(pSystemState.EntityManager);
        ecb.Dispose();
    }
}