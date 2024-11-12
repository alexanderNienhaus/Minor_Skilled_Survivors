using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public class PathfindingUnitParamsAuthoring : MonoBehaviour
{
    private class Baker : Baker<PathfindingUnitParamsAuthoring>
    {
        public override void Bake(PathfindingUnitParamsAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new PathfindingUnitParams { });
        }
    }
}

[BurstCompile]
public struct PathfindingUnitParams : IComponentData
{
    public float3 end;
}
