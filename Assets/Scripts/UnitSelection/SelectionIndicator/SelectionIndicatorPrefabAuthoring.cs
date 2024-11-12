using UnityEngine;
using Unity.Entities;
using Unity.Burst;

public class SelectionIndicatorPrefabAuthoring : MonoBehaviour
{
    [SerializeField] private GameObject selectionIndicatorPrefab;

    private class Baker : Baker<SelectionIndicatorPrefabAuthoring>
    {
        public override void Bake(SelectionIndicatorPrefabAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new SelectionIndicatorPrefab
            {
                prefab = GetEntity(pAuthoring.selectionIndicatorPrefab, TransformUsageFlags.Dynamic)
            });
        }
    }
}

[BurstCompile]
public struct SelectionIndicatorPrefab : IComponentData
{
    public Entity prefab;
}
