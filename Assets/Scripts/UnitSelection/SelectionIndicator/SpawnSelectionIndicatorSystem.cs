using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;

[BurstCompile]
public struct SelectionIndicatorData : ICleanupComponentData
{
    public Entity selectionIndicator;
}

[UpdateAfter(typeof(UnitSelectionSystem))]
public partial class SpawnSelectionIndicatorSystem : SystemBase
{
    private BeginInitializationEntityCommandBufferSystem beginInitializationEcbSystem;

    protected override void OnCreate()
    {        
        RequireForUpdate<SelectionIndicatorPrefab>();
        RequireForUpdate<SelectedUnitTag>();
        beginInitializationEcbSystem = World.GetExistingSystemManaged<BeginInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = beginInitializationEcbSystem.CreateCommandBuffer();

        foreach ((RefRO<LocalTransform> localTransform, Entity entity)
            in SystemAPI.Query<RefRO<LocalTransform>>().WithNone<SelectionIndicatorData>().WithAll<SelectedUnitTag>().WithEntityAccess())
        {
            Entity selectionIndicatorEntity = ecb.Instantiate(SystemAPI.GetSingleton<SelectionIndicatorPrefab>().prefab);
            SelectionIndicatorData selectionIndicatorData = new SelectionIndicatorData
            {
                selectionIndicator = selectionIndicatorEntity
            };
            ecb.AddComponent<SelectionIndicatorData>(entity);
            ecb.SetComponent(entity, selectionIndicatorData);            
            ecb.AddComponent<Parent>(selectionIndicatorEntity);
            ecb.SetComponent(selectionIndicatorEntity, new Parent { Value = entity });
        }
    }
}
