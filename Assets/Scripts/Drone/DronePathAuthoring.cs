using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public class DronePathAuthoring : MonoBehaviour
{
    private class Baker : Baker<DronePathAuthoring>
    {
        public override void Bake(DronePathAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddBuffer<DronePath>(entity);
        }
    }
}

[BurstCompile]
public struct DronePath : IBufferElementData
{
    public float3 pos;
}
