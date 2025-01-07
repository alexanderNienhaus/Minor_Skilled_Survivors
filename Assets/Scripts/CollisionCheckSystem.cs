using Unity.Entities;
using Unity.Physics;
using Unity.Jobs;
using Unity.Burst;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[BurstCompile]
public partial class CollisionCheckSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem endFixedEcbSystem;

    [BurstCompile]
    protected override void OnCreate()
    {
        endFixedEcbSystem = World.GetOrCreateSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = endFixedEcbSystem.CreateCommandBuffer();

        CollisionCheckJob triggerJob = new CollisionCheckJob
        {
            allSelectedUnits = GetComponentLookup<SelectedUnitTag>(true),
            allSelectionVolumes = GetComponentLookup<SelectionVolumeTag>(true),
            allBoids = GetComponentLookup<Boid>(true),
            allAttackables = GetComponentLookup<Attackable>(true),
            ecb = ecb
        };

        Dependency = triggerJob.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), Dependency);
        endFixedEcbSystem.AddJobHandleForProducer(Dependency);
        //Dependency.Complete();
    }
}