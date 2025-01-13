using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;

public class PathPositionAuthoring : MonoBehaviour
{
    private class Baker : Baker<PathPositionAuthoring>
    {
        public override void Bake(PathPositionAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddBuffer<PathPositions>(entity);
        }
    }
}

[BurstCompile]
[InternalBufferCapacity(0)]
public struct PathPositions : IBufferElementData
{
    public float3 pos;
}
