using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public class BaseAuthoring : MonoBehaviour
{
    private class Baker : Baker<BaseAuthoring>
    {
        public override void Bake(BaseAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Base
            {
                position = pAuthoring.transform.position
            });
        }
    }
}

[BurstCompile]
public struct Base : IComponentData
{
    public float3 position;
}

