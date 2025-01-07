using Unity.Entities;
using Unity.Physics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

[BurstCompile]
public partial struct CollisionCheckSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        EntityCommandBuffer ecb = new (Allocator.TempJob);

        CollisionCheckJob triggerJob = new ()
        {
            allSelectedUnits = systemState.GetComponentLookup<SelectedUnitTag>(true),
            allSelectionVolumes = systemState.GetComponentLookup<SelectionVolumeTag>(true),
            allBoids = systemState.GetComponentLookup<Boid>(true),
            allAttackables = systemState.GetComponentLookup<Attackable>(true),
            ecb = ecb,
            em = systemState.EntityManager
        };

        systemState.Dependency = triggerJob.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), systemState.Dependency);
        systemState.Dependency.Complete();
        ecb.Playback(systemState.EntityManager);
    }
}