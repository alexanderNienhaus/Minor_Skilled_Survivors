using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;

//[UpdateAfter(typeof(UnitSelectionSystem))]
public partial class SpawnSelectionIndicatorSystem : SystemBase
{
    private BeginInitializationEntityCommandBufferSystem beginInitializationEcbSystem;

    protected override void OnCreate()
    {        
        RequireForUpdate<SelectionIndicatorPrefab>();
        RequireForUpdate<SelectedUnitTag>();
    }

    protected override void OnUpdate()
    {
        beginInitializationEcbSystem = World.GetExistingSystemManaged<BeginInitializationEntityCommandBufferSystem>();
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
