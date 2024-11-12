using UnityEngine;
using Unity.Entities;
using Unity.Burst;

public class SelectedUnitTagAuthoring : MonoBehaviour
{
    private class Baker : Baker<SelectedUnitTagAuthoring>
    {
        public override void Bake(SelectedUnitTagAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SelectedUnitTag());
            SetComponentEnabled<SelectedUnitTag>(entity, false);
        }
    }
}

[BurstCompile]
public struct SelectedUnitTag : IComponentData, IEnableableComponent
{
}