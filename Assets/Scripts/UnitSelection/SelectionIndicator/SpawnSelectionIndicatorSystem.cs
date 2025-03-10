using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Collections;

[BurstCompile]
[UpdateAfter(typeof(UnitSelectionSystem))]
public partial struct SpawnSelectionIndicatorSystem : ISystem
{
    private EntityQuery query;

    [BurstCompile]
    public void OnCreate(ref SystemState pSystemState)
    {
        pSystemState.RequireForUpdate<SelectionIndicatorPrefab>();
        pSystemState.RequireForUpdate<SelectedUnitTag>();

        EntityQueryBuilder entityQueryBuilder = new(Allocator.Temp);
        entityQueryBuilder.WithAll<SelectedUnitTag>().WithNone<SelectionIndicatorData>();
        query = pSystemState.GetEntityQuery(entityQueryBuilder);
        entityQueryBuilder.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState pSystemState)
    {
        int count = query.CalculateEntityCount();
        if (count == 0)
            return;

        NativeArray<Entity> selectedUnitEntities = query.ToEntityArray(Allocator.Temp);

        NativeArray<Entity> instantiatedEntities = pSystemState.EntityManager.Instantiate(SystemAPI.GetSingleton<SelectionIndicatorPrefab>().prefab, count, Allocator.Temp);

        for (int i = 0; i < count; i++)
        {
            pSystemState.EntityManager.AddComponent<SelectionIndicatorData>(selectedUnitEntities[i]);
            pSystemState.EntityManager.SetComponentData(selectedUnitEntities[i], new SelectionIndicatorData { selectionIndicator = instantiatedEntities[i] });
            pSystemState.EntityManager.AddComponent<Parent>(instantiatedEntities[i]);
            pSystemState.EntityManager.SetComponentData(instantiatedEntities[i], new Parent { Value = selectedUnitEntities[i] });
        }

        instantiatedEntities.Dispose();
        selectedUnitEntities.Dispose();
    }
}
