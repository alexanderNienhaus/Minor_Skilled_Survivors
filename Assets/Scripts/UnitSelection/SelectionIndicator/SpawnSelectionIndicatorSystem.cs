using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;

[BurstCompile]
[UpdateAfter(typeof(UnitSelectionSystem))]
public partial struct SpawnSelectionIndicatorSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState pSystemState)
    {
        pSystemState.RequireForUpdate<SelectionIndicatorPrefab>();
        pSystemState.RequireForUpdate<SelectedUnitTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState pSystemState)
    {
        EntityQueryBuilder entityQueryBuilder = new(Allocator.Temp);
        entityQueryBuilder.WithAll<SelectedUnitTag>().WithNone<SelectionIndicatorData>();
        int count = pSystemState.GetEntityQuery(entityQueryBuilder).CalculateEntityCount();
        entityQueryBuilder.Dispose();
        NativeArray<Entity> selectedUnitEntities = new(count, Allocator.Temp);
        if (count == 0)
            return;

        count = 0;
        foreach ((RefRO<LocalTransform> localTransform, Entity entity)
            in SystemAPI.Query<RefRO<LocalTransform>>().WithNone<SelectionIndicatorData>().WithAll<SelectedUnitTag>().WithEntityAccess())
        {
            selectedUnitEntities[count] = entity;
            count++;
        }

        NativeArray<Entity> instantiatedEntities = pSystemState.EntityManager.Instantiate(SystemAPI.GetSingleton<SelectionIndicatorPrefab>().prefab,
            count, Allocator.Persistent);

        for (int i = 0; i < count; i++)
        {
            pSystemState.EntityManager.AddComponent<SelectionIndicatorData>(selectedUnitEntities[i]);
            pSystemState.EntityManager.SetComponentData(selectedUnitEntities[i], new SelectionIndicatorData
            {
                selectionIndicator = instantiatedEntities[i]
            });
            pSystemState.EntityManager.AddComponent<Parent>(instantiatedEntities[i]);
            pSystemState.EntityManager.SetComponentData(instantiatedEntities[i], new Parent { Value = selectedUnitEntities[i] });
        }

        instantiatedEntities.Dispose();
        selectedUnitEntities.Dispose();
    }
}
