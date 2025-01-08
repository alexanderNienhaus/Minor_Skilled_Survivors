using Unity.Entities;
using Unity.Physics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct MultipleUnitSelectionCheckSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState systemState)
    {
        systemState.RequireForUpdate<SelectedUnitTag>();
        systemState.RequireForUpdate<SelectionVolumeTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        EntityCommandBuffer ecb = new (Allocator.TempJob);

        MultipleUnitSelectionCheckJob triggerJob = new ()
        {
            allSelectedUnits = systemState.GetComponentLookup<SelectedUnitTag>(true),
            allSelectionVolumes = systemState.GetComponentLookup<SelectionVolumeTag>(true),
            ecb = ecb
        };

        systemState.Dependency = triggerJob.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), systemState.Dependency);
        systemState.Dependency.Complete();
        ecb.Playback(systemState.EntityManager);
    }
}