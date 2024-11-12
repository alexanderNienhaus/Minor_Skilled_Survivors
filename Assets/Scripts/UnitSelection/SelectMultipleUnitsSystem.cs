using Unity.Entities;
using Unity.Physics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial class SelectMultipleUnitsSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem endFixedEcbSystem;

    protected override void OnCreate()
    {
        //RequireForUpdate<SelectedUnitTag>();
        endFixedEcbSystem = World.GetOrCreateSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = endFixedEcbSystem.CreateCommandBuffer();

        CollisionTriggerJob triggerJob = new CollisionTriggerJob
        {
            allSelectedUnits = GetComponentLookup<SelectedUnitTag>(true),
            allSelectionVolumes = GetComponentLookup<SelectionVolumeTag>(true),
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

[BurstCompile]
public struct CollisionTriggerJob : ITriggerEventsJob
{
    [ReadOnly] public ComponentLookup<SelectedUnitTag> allSelectedUnits;
    [ReadOnly] public ComponentLookup<SelectionVolumeTag> allSelectionVolumes;
    public EntityCommandBuffer ecb;

    public void Execute(TriggerEvent pTriggerEvent)
    {
        Entity entityA = pTriggerEvent.EntityA;
        Entity entityB = pTriggerEvent.EntityB;

        if (allSelectedUnits.HasComponent(entityA) && allSelectedUnits.HasComponent(entityB))
        {
            return;
        }

        if (allSelectedUnits.HasComponent(entityA) && allSelectionVolumes.HasComponent(entityB))
        {
            ecb.SetComponentEnabled<SelectedUnitTag>(entityA, true);
        }
        else if (allSelectionVolumes.HasComponent(entityB) && allSelectionVolumes.HasComponent(entityA))
        {
            ecb.SetComponentEnabled<SelectedUnitTag>(entityB, true);
        }
    }
}
