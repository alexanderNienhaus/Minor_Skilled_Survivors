using Unity.Entities;
using Unity.Burst;

[BurstCompile]
[UpdateAfter(typeof(UnitSelectionSystem))]
public partial class DestroySelectionIndicatorSystem : SystemBase
{
    private BeginInitializationEntityCommandBufferSystem beginInitializationEcbSystem;

    protected override void OnCreate()
    {
        beginInitializationEcbSystem = World.GetExistingSystemManaged<BeginInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = beginInitializationEcbSystem.CreateCommandBuffer();

        foreach ((RefRO<SelectionIndicatorData> selectionIndicatorData, Entity entity)
            in SystemAPI.Query<RefRO<SelectionIndicatorData>>().WithNone<SelectedUnitTag>().WithEntityAccess())
        {
            ecb.RemoveComponent<SelectionIndicatorData>(entity);
            ecb.DestroyEntity(selectionIndicatorData.ValueRO.selectionIndicator);
        }
    }
}

