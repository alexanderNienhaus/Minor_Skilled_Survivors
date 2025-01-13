using Unity.Entities;
using Unity.Burst;

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial class MulipleUnitSelectionSystem : SystemBase
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
        if (SystemAPI.TryGetSingletonEntity<SelectionVolumeTag>(out Entity selectionEntity) && selectionEntity != Entity.Null
            && SystemAPI.HasComponent<StepsToLiveData>(selectionEntity))
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