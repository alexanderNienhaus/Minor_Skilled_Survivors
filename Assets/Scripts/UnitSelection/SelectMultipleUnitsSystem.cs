using Unity.Entities;
using Unity.Physics;
using Unity.Jobs;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial class SelectMultipleUnitsSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem endFixedEcbSystem;

    protected override void OnCreate()
    {
        endFixedEcbSystem = World.GetOrCreateSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = endFixedEcbSystem.CreateCommandBuffer();

        CollisionCheckJob triggerJob = new CollisionCheckJob
        {
            allSelectedUnits = GetComponentLookup<SelectedUnitTag>(true),
            allSelectionVolumes = GetComponentLookup<SelectionVolumeTag>(true),
            allBoids = GetComponentLookup<Boid>(true),
            allAttackableUnits = GetComponentLookup<AttackableUnit>(true),
            ecb = ecb
        };
        
        Dependency = triggerJob.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), Dependency);
        endFixedEcbSystem.AddJobHandleForProducer(Dependency);
        Dependency.Complete();

        if (SystemAPI.TryGetSingletonEntity<SelectionVolumeTag>(out Entity selectionEntity) && selectionEntity != Entity.Null && SystemAPI.HasComponent<StepsToLiveData>(selectionEntity))
        {
            StepsToLiveData stepsToLive = SystemAPI.GetComponent<StepsToLiveData>(selectionEntity);
            stepsToLive.value--;
            ecb.SetComponent(selectionEntity, stepsToLive);
            if (stepsToLive.value <= 0)
            {
                ecb.DestroyEntity(selectionEntity);
            }
        }
        else if (selectionEntity != Entity.Null)
        {
            ecb.AddComponent<StepsToLiveData>(selectionEntity);
            ecb.SetComponent(selectionEntity, new StepsToLiveData { value = 1 });
        }
    }
}