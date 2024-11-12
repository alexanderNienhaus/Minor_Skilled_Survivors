using UnityEngine;
using Unity.Entities;
using Unity.Burst;

public class PlacedObjectTagAuthoring : MonoBehaviour
{
    private class Baker : Baker<PlacedObjectTagAuthoring>
    {
        public override void Bake(PlacedObjectTagAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new PlacedObjectTag
            {
            });
        }
    }
}

[BurstCompile]
public struct PlacedObjectTag : IComponentData
{
    public int id;
}